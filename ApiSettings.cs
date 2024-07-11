namespace LocalEmbeddings;

public record ApiSettings(
    string ApiUrl,
    string ApiKey,
    string Model,
    string MarqoHost,
    string MarqoIndex,
    string MarqoModel,
    string? MarqoApiKey = null) : BaseSettings<ApiSettings>
{
    public static Task<ApiSettings> ReadSettings() => BaseSettings<ApiSettings>.ReadSettings(
        filename: "api-settings.json",
        getDefault: () => new ApiSettings("apiUrl", "apiKey", "model", "marqoUrl", "marqoIndex", "marqoModel"),
        isValid: settings => settings is
        {
            ApiUrl: not null,
            ApiKey: not null,
            Model: not null,
            MarqoHost: not null,
            MarqoIndex: not null,
            MarqoModel: not null 
        });
}