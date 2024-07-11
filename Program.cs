using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ConsoleMarkdownRenderer;
using ConsoleMarkdownRenderer.ObjectRenderers;
using Dasync.Collections;
using LocalEmbeddings;
using Markdig.Syntax;

class Program
{
    private const string IssueOptions = "Any key to return, Q to ask questions about the issue, or R to find related issues";

    static async Task Main(string[] args)
    {
        var apiSettings = await ApiSettings.ReadSettings();
        
        var doRefresh = args.Any(a => a.Contains("--refresh", StringComparison.OrdinalIgnoreCase));
        var reindexExisting = args.Any(a => a.Contains("--reindex", StringComparison.OrdinalIgnoreCase));

        if (!doRefresh && reindexExisting)
        {
            Console.WriteLine("Refresh must be selected if reindexing");
            return;
        }
        
        if (doRefresh)
        {
            await InitializeMarqoIndex(apiSettings.MarqoHost, apiSettings.MarqoIndex, apiSettings.MarqoModel);

            string folderPath = @"C:\temp\issues";

            await GithubIssueDownloader.GetIssues(folderPath);

            var files = Directory.GetFiles(folderPath, "*.markdown");

            var documents = await GetFileDataAndSaveSummary(files, apiSettings)
                .Select(v => new Document(Path.GetFileNameWithoutExtension(v.file), v.file, v.content, v.title, v.summary))
                .ToListAsync();

            await StoreEmbeddingsInMarqo(documents, apiSettings.MarqoHost, apiSettings.MarqoIndex, reindexExisting);
        }
        else
        {
            Console.WriteLine("Run with --refresh to refresh issues, summarise and index, add --reindex to reindex existing summaries");
            Console.WriteLine();
        }

        var stats = await GetIndexStats(apiSettings.MarqoHost, apiSettings.MarqoIndex);

        if (stats is {Backend: not null})
        {
            Console.WriteLine("Marqo server status:");
            Console.WriteLine($"Backend: Memory {Math.Round(stats.Backend.MemoryUsedPercentage, 2)}%, Storage {Math.Round(stats.Backend.StorageUsedPercentage, 2)}%");
            Console.WriteLine();
        }
            
        
        var indexes = await GetIndexList(apiSettings.MarqoHost);

        Console.WriteLine("Current Marqo indexes:");
        foreach (var index in indexes)
            Console.WriteLine(index);
        Console.WriteLine();
        Console.WriteLine($"Current index: {apiSettings.MarqoIndex}");
        if (stats is not null) 
            Console.WriteLine($"Documents: {stats.NumberOfDocuments}, Vectors: {stats.NumberOfVectors}");
        Console.WriteLine();

        var githubSettings = await GithubSettings.ReadSettings();

        Console.WriteLine("Search the issue database:");

        var queryText = Console.ReadLine()??string.Empty;
        var offset = 0;

        while (!string.IsNullOrWhiteSpace(queryText))
        {
            Console.WriteLine("Querying..");
            Console.WriteLine();

            var topMatches = await QueryMarqo(apiSettings.MarqoHost, apiSettings.MarqoIndex, queryText, offset);

            var summaryToQuery = queryText;
            var summaryTask = new Lazy<Task<string>>(() => GetSummaryOfMatches(summaryToQuery, topMatches, apiSettings.ApiUrl, apiSettings.ApiKey, apiSettings.Model));
            WriteIssuesMenu(topMatches);
            var key = Console.ReadKey();

            bool updatedQuery = false;
            while ((key.KeyChar >= '1' && key.KeyChar < topMatches.Count + '1') || key.Key is ConsoleKey.S or ConsoleKey.N)
            {
                Console.WriteLine();
                if (key.Key == ConsoleKey.S)
                {
                    Console.WriteLine("Summarising, please wait...");

                    var summaryOfMatches = await summaryTask.Value;

                    if (!string.IsNullOrWhiteSpace(summaryOfMatches)) Console.Clear();
                    WriteMarkdown(summaryOfMatches);
                } else if (key.Key == ConsoleKey.N)
                {
                    updatedQuery = true;
                    offset += topMatches.Count;
                }
                else
                {
                    var selected = topMatches.ElementAt(key.KeyChar - '1');

                    WriteMarkdown($"## {selected.Title}{Environment.NewLine}{Environment.NewLine}{selected.Summary}");
                    Console.WriteLine();
                    Console.WriteLine($"Issue location https://github.com/{githubSettings.Owner}/{githubSettings.Repo}/issues/{selected.Id}");
                    Console.WriteLine();
                    
                    Console.WriteLine(IssueOptions);
                    var actionKey = Console.ReadKey(intercept: true);
                    while (actionKey.Key is ConsoleKey.Q or ConsoleKey.R)
                    {
                        switch (actionKey.Key)
                        {
                            case ConsoleKey.Q:
                                await AskQuestionsAboutIssue(selected, apiSettings.ApiUrl, apiSettings.ApiKey, apiSettings.Model);
                                break;
                            case ConsoleKey.R:
                                queryText = selected.Title + " " + selected.Summary;
                                updatedQuery = true;
                                offset = 0;
                                break;
                        }

                        if (!updatedQuery)
                        {
                            Console.WriteLine(IssueOptions);
                            actionKey = Console.ReadKey();
                        }
                        else break;
                    }
                }
                if (updatedQuery) break;

                WriteIssuesMenu(topMatches);
                key = Console.ReadKey();
            }

            if (updatedQuery) continue;
            Console.WriteLine("Search again or enter to quit:");
            queryText = Console.ReadLine() ?? string.Empty;
            offset = 0;
        }

        return;

        // todo: we're not really taking advantage of concurrency here
        static async IAsyncEnumerable<(string file, string content, string title, string summary)> GetFileDataAndSaveSummary(IEnumerable<string> files, ApiSettings apiSettings, int llmConcurrency = 1)
        {
            var semaphore = new SemaphoreSlim(llmConcurrency);
            var sw = new Stopwatch();
            foreach (var file in files)
            {
                string content = await File.ReadAllTextAsync(file);
                Regex.Replace(content, "\\(data:image/\\w+;base64,[^\\)]+\\)", "()");
                string title = content.Split(Environment.NewLine).FirstOrDefault()?.Substring(2) ?? "";

                var summaryFile = $"{file}.summary";

                if (File.Exists(summaryFile) && File.GetLastWriteTimeUtc(summaryFile) >= File.GetLastWriteTimeUtc(file))
                {
                    string summary = await File.ReadAllTextAsync(summaryFile);
                    yield return (file, content, title, summary);
                    continue;
                }

                await semaphore.WaitAsync();

                sw.Start();
                Console.WriteLine("Creating summary for " + file);
                try
                {
                    var summary = await GetSummary(content, apiSettings.ApiUrl, apiSettings.ApiKey, apiSettings.Model);
                    await File.WriteAllTextAsync(summaryFile, summary);

                    sw.Stop();
                    Console.WriteLine($"Created summary for {file} in {sw.Elapsed}");
                    yield return (file, content, title, summary);
                }
                finally
                {
                    sw.Stop();
                    sw.Reset();
                    semaphore.Release();
                }
            }
        }

        static void WriteIssuesMenu(List<Hit> topMatches)
        {
            Console.WriteLine();
            Console.WriteLine("Top matches:");
            foreach (var (match, index) in topMatches.Select((m, i) => (m, i)))
                Console.WriteLine($"{index + 1}: {match.Id}: {match.Title}");

            Console.WriteLine();
            Console.WriteLine("Hit S to display a summary of these issues, or N to display the next page of results");
            Console.WriteLine("Any other key to start a new search");
        }
    }

    private static async Task AskQuestionsAboutIssue(Hit selected, string apiUrl, string apiKey, string model)
    {
        Console.WriteLine("Ask a question about this issue, enter to return:");
        var questionText = Console.ReadLine()??string.Empty;
        Message[]? messages = null;
        while (!string.IsNullOrWhiteSpace(questionText))
        {
            Console.WriteLine();
            Console.WriteLine("Querying, please wait..");
            Console.WriteLine();
            string issueBody = selected.Content;

            messages = messages == null
                ? GetInitialQuestionMessages(questionText, issueBody)
                : messages.Append(new Message("user", questionText)).ToArray();

            (var reply, messages) = await AnswerQuestionAboutIssue(apiUrl, apiKey, model, messages);
            WriteMarkdown(reply);
            Console.WriteLine();
            Console.WriteLine("Keep asking questions or hit enter to return");
            questionText = Console.ReadLine()??string.Empty;
        }
    }

    private static async Task InitializeMarqoIndex(string host, string index, string model)
    {
        var createIndexUrl = $"{host}/indexes/{index}";
        using var client = new HttpClient();
        var requestBody = new
        {
            type = "unstructured",
            model,
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await client.PostAsync(createIndexUrl, content);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
        {
            throw new Exception($"Failed to initialize index. Status code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
        }
        Console.WriteLine("Index initialized successfully.");
    }
    
    //
    // private static async Task<List<float>> GetEmbedding(string text, string apiUrl, string apiKey, string model)
    // {
    //     using (var client = new HttpClient())
    //     {
    //         client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
    //
    //         var requestBody = new
    //         {
    //             input = new[] { text.Replace("\n", " ").Replace("\r", " ") },
    //             model = model
    //         };
    //
    //         var json = JsonSerializer.Serialize(requestBody);
    //         var content = new StringContent(json, Encoding.UTF8, "application/json");
    //
    //         var response = await client.PostAsync(apiUrl, content);
    //         response.EnsureSuccessStatusCode();
    //
    //         var responseBody = await response.Content.ReadAsStringAsync();
    //         var responseData = JsonSerializer.Deserialize<EmbeddingResponse>(responseBody);
    //
    //         return responseData?.Data[0].Embedding??new();
    //     }
    // }

    
    private static async Task<string> GetSummaryOfMatches(string search, List<Hit> matches, string apiUrl, string apiKey, string model)
    {
        try 
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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
                            "Please give a short summary of all of the above issues, with one bullet point per issue, and how they relate to this search: " +
                            search
                    }).ToArray(),
                model,
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{apiUrl}/chat/completions", content);

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
    
    private static async Task<string> GetSummary(string text, string apiUrl, string apiKey, string model)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

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
                model,
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{apiUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<ChatCompletion>(responseBody, JsonSerializerOptions);

            return responseData?.Choices[0].Message.Content??string.Empty;
        }
    }

    private static Message[] GetInitialQuestionMessages(string question, string issueBody)
    {
        return [
            new Message("system", "You are a helpful assistant who specialises in answering questions about github issues, which include details of features for an educational software package. Answer the question as best you can with the details in the issue, as succinctly as possible, without adding anything you are unsure about"),
            new Message("user", $"Question: {question}\n Issue: {issueBody.Replace("\n", " ").Replace("\r", "")}")
        ];
    }

    private static async Task<(string, Message[])> AnswerQuestionAboutIssue(string apiUrl, string apiKey, string model, Message[] messages)
    {
        using (var client = new HttpClient())
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

            var requestBody = new { messages, model };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync($"{apiUrl}/chat/completions", content);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync();
            var responseData = JsonSerializer.Deserialize<ChatCompletion>(responseBody, JsonSerializerOptions);

            if (responseData is {Choices.Count: > 0}) messages = messages.Append(responseData.Choices[0].Message).ToArray();
            return (responseData?.Choices[0].Message.Content??string.Empty, messages);
        }
    }

    private static async Task StoreEmbeddingsInMarqo(List<Document> allDocuments, string marqoHost, 
        string index, bool reindexExisting)
    {
        using (var client = new HttpClient())
        {
            const int batch = 25;
            var documents = allDocuments.Take(batch).ToList();
            int offset = 0;
            while (documents.Any())
            {
                if (!reindexExisting)
                {
                    var missingDocIdBag = new ConcurrentBag<string>();
                    await documents.ParallelForEachAsync(async document =>
                    {
                        var res = await client.GetAsync($"{marqoHost}/indexes/{index}/documents/{document._id}");

                        if (res.IsSuccessStatusCode)
                        {
                            var serverDoc = JsonSerializer.Deserialize<Document>(await res.Content.ReadAsStringAsync());
                            if (serverDoc?.content.Equals(document.content, StringComparison.OrdinalIgnoreCase)??false) return;
                        }

                        missingDocIdBag.Add(document._id);
                    });
                    documents = documents.IntersectBy(missingDocIdBag, d => d._id).ToList();
                }
                
                if (documents.Count > 0)
                {
                    var requestBody = new StoreDocumentMarqoRequest(documents, ["title", "summary"]);

                    var json = JsonSerializer.Serialize(requestBody);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync($"{marqoHost}/indexes/{index}/documents", content);

                    // this was a failed attempt to use "useExistingTensors = true" to avoid reindexing everything
                    // it threw a 500 error so i'd fallback, but it did it for every row so i'm not using it anymore 
                    
                    // if the summary has changed and we're not re-indexing, it'll throw an exception. in this case re-index
                    // if (!response.IsSuccessStatusCode && response.StatusCode == HttpStatusCode.InternalServerError &&
                    //     !reindexExisting)
                    // {
                    //     Console.WriteLine("Chunks don't match, attempting re-indexing..");
                    //     requestBody = requestBody with { useExistingTensors = false };
                    //     json = JsonSerializer.Serialize(requestBody);
                    //     content = new StringContent(json, Encoding.UTF8, "application/json");
                    //     response = await client.PostAsync($"{marqoHost}/indexes/{index}/documents", content);
                    // }

                    response.EnsureSuccessStatusCode();

                    Console.WriteLine($"Successfully updated/uploaded {documents.Count} documents");
                }         
                offset++;
                documents = allDocuments.Skip(offset * batch).Take(batch).ToList();
            }
            
        }
    }
    private static async Task<List<Hit>> QueryMarqo(string marqoHost, string index, string query, int offset)
    {
        using var client = new HttpClient();
        var requestBody = new
        {
            q = query,
            limit = 8,
            showHighlights = true,
            searchMethod = "TENSOR",
            offset,
            attributesToRetrieve = new[] { "id", "title", "summary", "content" }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync($"{marqoHost}/indexes/{index}/search", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<MarqoSearchResponse>(responseBody, JsonSerializerOptions);

        return searchResults?.Hits??new();
    }

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    private static void WriteMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return;

        markdown = markdown.Replace("\r", "");
        markdown = Regex.Replace(markdown, "(?<!\n)\n(?!\n)", "\n\n\n");
        
        Displayer.DisplayMarkdown(markdown, new Uri(AppContext.BaseDirectory, UriKind.Absolute), allowFollowingLinks: false);

    }
    private static async Task<MarqoStats?> GetIndexStats(string marqoHost, string index)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync($"{marqoHost}/indexes/{index}/stats");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<MarqoStats>(responseBody, JsonSerializerOptions);
        return results;
    }
    private static async Task<string[]> GetIndexList(string marqoHost)
    {
        using var client = new HttpClient();

        var response = await client.GetAsync(marqoHost + "/indexes");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<Indexes>(responseBody, JsonSerializerOptions);
        return results?.Results.Select(v => v.IndexName).ToArray()??[];
    }

    private record Indexes(Index[] Results);
    private record Index(string IndexName);
    public record Choice(int Index, Message Message, string FinishReason);
    public record Message(
        [property: JsonPropertyName("role")] string Role, 
        [property: JsonPropertyName("content")]string Content);

    public record Usage(int PromptTokens, int CompletionTokens, int TotalTokens);

    private record MarqoSearchResponse(List<Hit> Hits);
    private record Backend(
        [property: JsonPropertyName("memoryUsedPercentage")] double MemoryUsedPercentage,
        [property: JsonPropertyName("storageUsedPercentage")] double StorageUsedPercentage
    );

    private record MarqoStats(
        [property: JsonPropertyName("numberOfDocuments")] int NumberOfDocuments,
        [property: JsonPropertyName("numberOfVectors")] int NumberOfVectors,
        [property: JsonPropertyName("backend")] Backend Backend
    );
    public record ChatCompletion(
        string Id,
        string Object,
        long Created,
        string Model,
        List<Choice> Choices,
        Usage Usage);

    private record Hit(string Id, float Score, List<Highlight> Highlights, string Content, string Summary, string Title)
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = Id;

        [JsonPropertyName("_score")]
        public float Score { get; set; } = Score;

        [JsonPropertyName("_highlights")]
        public List<Highlight> Highlights { get; set; } = Highlights;

        [JsonPropertyName("content")]
        public string Content { get; set; } = Content;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = Summary;

        [JsonPropertyName("title")]
        public string Title { get; set; } = Title;
    }
    private record Highlight(string Content);
    
    record Document(string id, string filename, string content, string title, string summary)
    {
        public string id { get; } = id;
        public string _id { get; } = id;
        public string filename { get; } = filename;
        public string content { get; } = content;
        public string title { get; } = title;
        public string summary { get; } = summary;
    }
    private record StoreDocumentMarqoRequest(List<Document> documents, List<string> tensorFields);
}


