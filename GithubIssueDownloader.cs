using Octokit;

namespace LocalEmbeddings;

public interface IMarkdownFileDownloader
{
    Task GetIssues(string folder);
}

public class GithubIssueDownloader : IMarkdownFileDownloader
{
    private readonly GithubSettings _githubSettings;

    public GithubIssueDownloader(GithubSettings githubSettings)
    {
        _githubSettings = githubSettings;
    }

    public async Task GetIssues(string folder)
    {
        if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

        var client = new GitHubClient(new ProductHeaderValue("GitHubIssueDownloader"))
        {
            Credentials = new Credentials(_githubSettings.GitHubToken)
        };

        await GetAllIssues(client, _githubSettings.Owner, _githubSettings.Repo, folder);

    }
    
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
                string filename = Path.Join(folder, $"{pr.Number}.markdown");
                if (File.Exists(filename)) 
                    File.Delete(filename);
                else continue;

                filename = $"{filename}.summary";
                if (File.Exists(filename)) File.Delete(filename);

                Console.WriteLine($"Removed PR #{pr.Number}");
            }

            var anyNewOrUpdated = false;
            var issuesToSave = issues.Where(i => i is {PullRequest: null}).ToList();
            foreach (var issue in issuesToSave)
            {
                string filename = Path.Join(folder, $"{issue.Number}.markdown");
                if (File.Exists(filename))
                {
                    if (!issue.UpdatedAt.HasValue) continue;
                    if (issue.UpdatedAt.Value.UtcDateTime < File.GetLastWriteTimeUtc(filename)) continue;
                }
                anyNewOrUpdated = true;
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

                Console.WriteLine($"Saved issue #{issue.Number} to {filename}");
            }

            if (issuesToSave.Count > 1 && !anyNewOrUpdated) break;
            page++;
        }
    }
}