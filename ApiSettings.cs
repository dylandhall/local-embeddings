namespace LocalEmbeddings;

public record ApiSettings(
    List<LlmApiConfig> Apis,
    string DbHost,
    string DbIndex,
    string DbEmbeddingModel,
    string LocalFolder,
    string? DbApiKey = null) : BaseSettings<ApiSettings>
{
    public static Task<ApiSettings> ReadSettings() => BaseSettings<ApiSettings>.ReadSettings(
        filename: "api-settings.json",
        getDefault: () => new ApiSettings([new("apiUrl", "apiKey", "model")], "dbUrl", "dbIndex", "dbModel", "C:\\temp\\issues"),
        isValid: settings => settings is
        {
            Apis: {Count: > 0},
            DbHost: not null,
            DbIndex: not null,
            DbEmbeddingModel: not null,
            LocalFolder: not null
        } && settings.Apis.All(a => a.IsValid));
}

public record LlmApiConfig(
    string ApiUrl,
    string ApiKey,
    string Model)
{
    public bool IsValid => this is { ApiUrl: not null, Model: not null };
}