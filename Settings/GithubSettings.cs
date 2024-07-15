namespace LocalEmbeddings.Settings;

public record GithubSettings(string Owner, string Repo, string GitHubToken) : BaseSettings<GithubSettings>
{
    public static Task<GithubSettings> ReadSettings() => BaseSettings<GithubSettings>.ReadSettings(
        filename: "github-settings.json",
        getDefault: () =>
        {
            var owner = "owner";
            var repo = "repo";
            var gitHubToken = "token";
            
            Console.WriteLine($"Please enter the owner of the repository you'd like to index, enter for default ({owner}): ");
            var res = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(res)) owner = res;
            Console.WriteLine($"Please enter the name of the repository you'd like to index, enter for default ({repo}): ");
            res = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(res)) repo = res;
            Console.WriteLine($"Please enter your github token, enter for default ({gitHubToken}): ");
            res = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(res)) gitHubToken = res;

            return new GithubSettings(owner, repo, gitHubToken);
        },
        isValid: settings => settings is { GitHubToken: not null, Owner: not null, Repo: not null });
}