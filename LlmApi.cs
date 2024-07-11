using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LocalEmbeddings;

public interface ILlmApi: IDisposable
{
    Task<string> GetSummary(string text);
    Task<(string, Message[])> AnswerQuestionAboutIssue(Message[] messages);
    Task<string> GetSummaryOfMatches(string search, List<Hit> matches);
}

public class LlmApi: ILlmApi
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
    public async Task<string> GetSummary(string text)
    {
        var requestBody = new
        {
            messages = new[]
            { 
                new
                {
                    role="system",
                    content="You are going to be given github issue, which specifies a feature or describes a bug for an educational software package called maths pathway. You are required to summarise it for later searching. You need to include the names of the affected parts of the system and a short but detailed summary of the things that are being changed in the ticket. Try as hard as possible to include all detail without including extraneous or generic details."
                },
                new 
                {
                    role = "user",
                    content = text.Replace("\n", " ").Replace("\r", "")
                }
            },
            model = _apiSettings.Model,
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await Client.PostAsync($"{_apiSettings.ApiUrl}/chat/completions", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize<ChatCompletion>(responseBody, JsonSerializerOptions);

        return responseData?.Choices[0].Message.Content??string.Empty;
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);


    public async Task<(string, Message[])> AnswerQuestionAboutIssue(Message[] messages)
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

    public async Task<string> GetSummaryOfMatches(string search, List<Hit> matches)
    {
        try 
        {
            var requestBody = new
            {
                //input = new[] { text.Replace("\n", " ").Replace("\r", " ") },
                messages = new[]
                    {
                        new
                        {
                            role = "system",
                            content =
                                "You are a helpful assistant who searches through a database of issues for a user. The user will give you issues, then ask you a question, you will give a short summary to the user explaining how the issues relate to the user's search"
                        },
                    }.Concat(matches.Select(m =>
                        new
                        {
                            role = "user",
                            content = m.Summary
                        }).ToArray())
                    .Append(new
                    {
                        role = "user",
                        content =
                            "Please give a short summary of all of the above issues, with one bullet point per issue. Please also comment on how the issues relate to each other (particularly if they are bugs), and how they relate to this search: " +
                            search
                    }).ToArray(),
                _apiSettings.Model,
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await Client.PostAsync($"{_apiSettings.ApiUrl}/chat/completions", content);

            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<ChatCompletion>(responseBody, JsonSerializerOptions);

            return responseData?.Choices[0].Message.Content ?? string.Empty;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Cannot access llm server: {e.Message}");
            return string.Empty;
        }
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
