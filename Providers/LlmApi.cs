using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LocalEmbeddings.Models;
using LocalEmbeddings.Settings;

namespace LocalEmbeddings.Providers;

public class LlmApi(ApiSettings apiSettings) : ILlmApi
{
    private HttpClient? _client;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    
    // i've included llm apis as an array, so i can later introduce different llms for different tasks
    // eg asking questions on an expensive high-fidelity api, but summarising for indexing on a low-cost api
    // currently we're just using the first entry for everything but updating won't break config files
    private string? _model;
    private string Model => _model ??= apiSettings.Apis.FirstOrDefault()?.Model ?? string.Empty;
    private string? _apiUrl;
    private string ApiUrl => _apiUrl ??= apiSettings.Apis.FirstOrDefault()?.ApiUrl ?? throw new Exception("API Url not found, cannot connect to LLM server");
    
    private HttpClient Client
    {
        get
        {
            if (_client != null) return _client;
            var apiKey = apiSettings.Apis.FirstOrDefault()?.ApiKey;

            _client = new HttpClient();
            
            if (!string.IsNullOrWhiteSpace(apiKey))
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            return _client;
        }
    }

    public async Task<(string, Message[])> GetCompletion(Message[] messages)
    {
        try
        {
            var requestBody = new { messages, Model };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Client.PostAsync($"{ApiUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<ChatCompletion>(responseBody, JsonSerializerOptions);

            if (responseData is { Choices.Count: > 0 })
                messages = messages.Append(responseData.Choices[0].Message).ToArray();
            return (responseData?.Choices[0].Message.Content ?? string.Empty, messages);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return (string.Empty, []);
        }
    }


    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}