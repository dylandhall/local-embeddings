using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record Document(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("created")] long? CreatedTimestamp = null,
    [property: JsonPropertyName("closed")] long? ClosedTimestamp = null,
    [property: JsonPropertyName("updated")] long? UpdatedTimestamp = null
) : IDocument
{
    [JsonPropertyName("_id")]
    public string _id => Id;
    
    public DateTimeOffset? CreatedAt => CreatedTimestamp != null ? DateTimeOffset.FromUnixTimeMilliseconds(CreatedTimestamp.Value) : null;
    public DateTimeOffset? ClosedAt => ClosedTimestamp != null ? DateTimeOffset.FromUnixTimeMilliseconds(ClosedTimestamp.Value) : null;
    public DateTimeOffset? UpdatedAt => UpdatedTimestamp != null ? DateTimeOffset.FromUnixTimeMilliseconds(UpdatedTimestamp.Value) : null;
}