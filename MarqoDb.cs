using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dasync.Collections;

namespace LocalEmbeddings;

public interface IVectorDb: IDisposable
{
    Task StoreEmbeddings(List<Document> allDocuments, bool reindexExisting);
    Task<List<IDocument>> Query(string query, int offset);
    Task InitializeIndex();
    Task<IndexStats?> GetIndexStats();
    Task<string[]> GetIndexList();
}

public class MarqoDb : IVectorDb
{
    private readonly ApiSettings _apiSettings;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);

    public MarqoDb(ApiSettings apiSettings)
    {
        _apiSettings = apiSettings;
    }

    private HttpClient? _client;

    private HttpClient Client
    {
        get
        {
            if (_client != null) return _client;
         
            _client = new HttpClient();

            if (!string.IsNullOrEmpty(_apiSettings.MarqoApiKey))
            {
                _client.DefaultRequestHeaders.Add("x-api-key", _apiSettings.MarqoApiKey);
            }

            return _client;
        }
    }

    public async Task StoreEmbeddings(List<Document> allDocuments, bool reindexExisting)
    {
        var marqoHost = _apiSettings.MarqoHost;
        var index = _apiSettings.MarqoIndex;

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
                    var res = await Client.GetAsync($"{marqoHost}/indexes/{index}/documents/{document._id}");

                    if (res.IsSuccessStatusCode)
                    {
                        var serverDoc = JsonSerializer.Deserialize<Document>(await res.Content.ReadAsStringAsync());
                        if (serverDoc?.Content.Equals(document.Content, StringComparison.OrdinalIgnoreCase)??false) return;
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

                var response = await Client.PostAsync($"{marqoHost}/indexes/{index}/documents", content);

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
    public async Task<List<IDocument>> Query(string query, int offset)
    {
        var marqoHost = _apiSettings.MarqoHost;
        var index = _apiSettings.MarqoIndex;

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

        var response = await Client.PostAsync($"{marqoHost}/indexes/{index}/search", content);
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var searchResults = JsonSerializer.Deserialize<MarqoSearchResponse>(responseBody, JsonSerializerOptions);

        return searchResults?.Hits.ToList<IDocument>()??new();
    }


    public async Task InitializeIndex()
    {
        var host = _apiSettings.MarqoHost;
        var index = _apiSettings.MarqoIndex;
        var model = _apiSettings.MarqoModel;

        var createIndexUrl = $"{host}/indexes/{index}";

        var requestBody = new
        {
            type = "unstructured",
            model,
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        
        var response = await Client.PostAsync(createIndexUrl, content);
        if (!response.IsSuccessStatusCode && response.StatusCode != HttpStatusCode.Conflict)
        {
            throw new Exception($"Failed to initialize index. Status code: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
        }
        Console.WriteLine("Index initialized successfully.");
    }
    
    

    public async Task<IndexStats?> GetIndexStats()
    {
        string marqoHost = _apiSettings.MarqoHost;
        string index = _apiSettings.MarqoIndex;

        var response = await Client.GetAsync($"{marqoHost}/indexes/{index}/stats");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<IndexStats>(responseBody, JsonSerializerOptions);
        return results;
    }
    public async Task<string[]> GetIndexList()
    {
        string marqoHost = _apiSettings.MarqoHost;
        var response = await Client.GetAsync(marqoHost + "/indexes");
        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var results = JsonSerializer.Deserialize<Indexes>(responseBody, JsonSerializerOptions);
        return results?.Results.Select(v => v.IndexName).ToArray()??[];
    }


    public void Dispose()
    {
        _client?.Dispose();
        GC.SuppressFinalize(this);
    }
}
public record MarqoSearchResponse(List<Hit> Hits);
public record Backend(
    [property: JsonPropertyName("memoryUsedPercentage")] double MemoryUsedPercentage,
    [property: JsonPropertyName("storageUsedPercentage")] double StorageUsedPercentage
);

public record IndexStats(
    [property: JsonPropertyName("numberOfDocuments")] int NumberOfDocuments,
    [property: JsonPropertyName("numberOfVectors")] int NumberOfVectors,
    [property: JsonPropertyName("backend")] Backend Backend
);

public interface IDocument
{
    string Id { get; }
    string Content { get; }
    string Title { get; }
    string Summary { get; }
}

public record Hit(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("_score")] float Score,
    [property: JsonPropertyName("_highlights")] List<Highlight> Highlights,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("title")] string Title
) : IDocument;
public record Highlight(string Content);

public record Indexes(Index[] Results);
public record Index(string IndexName);
public record Document(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary
) : IDocument
{
    [JsonPropertyName("_id")]
    public string _id => Id;
}
public record StoreDocumentMarqoRequest(List<Document> documents, List<string> tensorFields);