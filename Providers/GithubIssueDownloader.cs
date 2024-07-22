using System.Text.Json;
using System.Text.Json.Serialization;
using LocalEmbeddings.Settings;
using Octokit;

namespace LocalEmbeddings.Providers;

public class GithubIssueDownloader(GithubSettings githubSettings, IProgramSettings programSettings)
    : IDocumentFileDownloader
{
    private readonly bool _fullGithubRefresh = programSettings.FullGithubRefresh;

    public async Task GetIssues(string folder)
    {
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var client = new GitHubClient(new ProductHeaderValue("GitHubIssueDownloader"))
        {
            Credentials = new Credentials(githubSettings.GitHubToken)
        };

        await GetAllIssues(client, githubSettings.Owner, githubSettings.Repo, folder);

    }

    public string GetUrlForDocument(string id) =>
        $"https://github.com/{githubSettings.Owner}/{githubSettings.Repo}/issues/{id}";

    public string GetFileName(string id) => $"{id}.markdown";
    public string GetMetadataFilename(string id) => $"{id}.metadata";
    public string GetSummaryFileName(string originalFilename) => originalFilename + ".summary";
    public string FileNameMask => "*.markdown";

    private async Task GetAllIssues(GitHubClient client, string owner, string repo, string folder)
    {
        var issueRequest = new RepositoryIssueRequest { State = ItemStateFilter.All, SortDirection = SortDirection.Descending, SortProperty = IssueSort.Created };

        int page = 1;

        while (true)
        {
            var issues = await client.Issue.GetAllForRepository(owner, repo, issueRequest, new ApiOptions { PageSize = 30, PageCount = 1, StartPage = page });

            if (issues.Count == 0) break;

            // exists because i didn't originally filter out PRs and a summary of 10 thousand PRs checklists would be a waste of time

            foreach (var pr in issues.Where(i => i is {PullRequest: not null}))
            {
                string filename = Path.Join(folder, GetFileName(pr.Number.ToString()));
                if (File.Exists(filename)) 
                    File.Delete(filename);
                else continue;

                filename = GetSummaryFileName(filename);
                if (File.Exists(filename)) File.Delete(filename);

                Console.WriteLine($"Removed PR #{pr.Number}");
            }

            var anyNewOrUpdated = false;
            var issuesToSave = issues.Where(i => i is {PullRequest: null}).ToList();
            // not doing concurrently, i'll just hit the github api limit
            foreach (var issue in issuesToSave)
            {
                string filename = Path.Join(folder, GetFileName(issue.Number.ToString()));
                string metaDataFilename = Path.Join(folder, GetMetadataFilename(issue.Number.ToString()));
                
                var b = !File.Exists(metaDataFilename);
                await File.WriteAllTextAsync(metaDataFilename, JsonSerializer.Serialize(new Metadata(issue.CreatedAt, issue.UpdatedAt, issue.ClosedAt, issue.GetAssigneeNames())));
                if (File.Exists(filename))
                {
                    if (b)
                    {
                        //await File.WriteAllTextAsync(metaDataFilename, JsonSerializer.Serialize(new GithubIssueMetadata(issue.CreatedAt, issue.UpdatedAt, issue.ClosedAt, issue.GetAssigneeNames())));
                        anyNewOrUpdated = true;
                    }

                    if (!issue.UpdatedAt.HasValue) continue;
                    if (issue.UpdatedAt.Value.UtcDateTime < File.GetLastWriteTimeUtc(filename)) continue;
                } else if (File.Exists(GetSummaryFileName(filename)))
                    File.Delete(GetSummaryFileName(filename));

                anyNewOrUpdated = true;

                //var writeMetadata = File.WriteAllTextAsync(metaDataFilename, JsonSerializer.Serialize(new GithubIssueMetadata(issue.CreatedAt, issue.UpdatedAt, issue.ClosedAt, issue.GetAssigneeNames())));
                await using (StreamWriter writer = new StreamWriter(filename))
                {
                    await writer.WriteLineAsync($"# {issue.Title}");

                    if (issue.Labels.Count > 0)
                    {
                        List<string> labels = new List<string>();
                        foreach (var label in issue.Labels)
                        {
                            labels.Add(label.Name);
                        }
                        await writer.WriteLineAsync($"Labels: {string.Join(", ", labels)}{Environment.NewLine}");
                    }
                    await writer.WriteLineAsync($"Date added: {issue.CreatedAt.LocalDateTime.ToShortDateString()}{Environment.NewLine}");

                    await writer.WriteLineAsync(issue.Body);
                }

                //await writeMetadata;
                Console.WriteLine($"Saved issue #{issue.Number} to {filename}");
            }

            // this bails if we're going back in time and we've gotten to a point where we've looked at 30 issues and
            // none have been updated, assuming we've gotten to a historical point in the database.
            // bypass with --full-github-refresh on the command line
            if (!_fullGithubRefresh && issuesToSave.Count > 1 && !anyNewOrUpdated) break;
            page++;
        }
    }
}

public record Metadata(
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ClosedAt,
    string[] Assignees)
{
    public bool Closed => ClosedAt.HasValue;
}

internal static class IssueExt
{
    public static string[] GetAssigneeNames(this Issue issue) => issue.Assignees.Select(u => u.Login ?? u.Email).ToArray();
}