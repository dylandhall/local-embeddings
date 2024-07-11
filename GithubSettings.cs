using System.Text.Json;

namespace LocalEmbeddings;

public record GithubSettings(string Owner, string Repo, string GitHubToken) : BaseSettings<GithubSettings>
{
    public static Task<GithubSettings> ReadSettings() => BaseSettings<GithubSettings>.ReadSettings(
        filename: "github-settings.json",
        getDefault: () => new GithubSettings("owner", "repo", "token"),
        isValid: settings => settings is { GitHubToken: not null, Owner: not null, Repo: not null });
}