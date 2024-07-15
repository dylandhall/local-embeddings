using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record Hit(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("_score")] float Score,
    [property: JsonPropertyName("_highlights")] List<Highlight> Highlights,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("title")] string Title
) : IDocument;