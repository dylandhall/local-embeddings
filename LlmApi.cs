using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LocalEmbeddings;


public interface ILlmApi: IDisposable
{
    Task<(string, Message[])> GetCompletion(Message[] messages);
}

public class LlmApi : ILlmApi
{
    public LlmApi(ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;
    }

    private readonly ApiSettings _apiSettings;
    private HttpClient? _client;

    private HttpClient Client
    {
        get
        {
            if (_client != null) return _client;
            _client = new HttpClient();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiSettings.ApiKey);

            return _client;
        }
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);


    public async Task<(string, Message[])> GetCompletion(Message[] messages)
    {
        var requestBody = new { messages, _apiSettings.Model };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync($"{_apiSettings.ApiUrl}/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<ChatCompletion>(responseBody, JsonSerializerOptions);

        if (responseData is {Choices.Count: > 0}) messages = messages.Append(responseData.Choices[0].Message).ToArray();
        return (responseData?.Choices[0].Message.Content??string.Empty, messages);
    }


    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}


public record ChatCompletion(
    string Id,
    string Object,
    long Created,
    string Model,
    List<Choice> Choices,
    Usage Usage);


public record Choice(int Index, Message Message, string FinishReason);


public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);
