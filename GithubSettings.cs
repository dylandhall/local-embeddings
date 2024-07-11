using System.Text.Json;

namespace LocalEmbeddings;

public record GithubSettings(string Owner, string Repo, string GitHubToken)
{
    private static GithubSettings? _settings;
    public static async Task<GithubSettings> ReadSettings()
    {
        if (_settings != default) return _settings;

        var settingsFile = Path.Join(AppContext.BaseDirectory, "github-settings.json");
        if (!File.Exists(settingsFile))
        {
            await File.WriteAllTextAsync(settingsFile, JsonSerializer.Serialize(new GithubSettings("owner", "repo", "token")));
            
            Console.WriteLine($"Github settings not found, please update the settings file at {settingsFile} and run again");

            throw new Exception("Cannot find settings");
        }

        var settings = JsonSerializer.Deserialize<GithubSettings>(await File.ReadAllTextAsync(settingsFile));

        if (settings is null or {GitHubToken:null} or {Owner:null} or {Repo:null})
            throw new Exception("Github settings not valid");

        _settings = settings;
        return settings;
    }
}