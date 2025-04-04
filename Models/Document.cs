using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record Document(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("filename")] string Filename,
    [property: JsonPropertyName("content")] string Content,
    [property: JsonPropertyName("title")] string Title,
    [property: JsonPropertyName("summary")] string Summary,
    [property: JsonPropertyName("created")] long CreatedTimestamp = 0,
    [property: JsonPropertyName("closed")] long ClosedTimestamp = 0,
    [property: JsonPropertyName("updated")] long UpdatedTimestamp = 0
) : IDocument
{
    [JsonPropertyName("_id")]
    public string _id => Id;

    [JsonIgnore]
    public DateTimeOffset? CreatedAt => CreatedTimestamp > 0 
        ? DateTimeOffset.FromUnixTimeMilliseconds(CreatedTimestamp) 
        : null;
    
    [JsonIgnore]
    public DateTimeOffset? ClosedAt => ClosedTimestamp > 0 
        ? DateTimeOffset.FromUnixTimeMilliseconds(ClosedTimestamp) 
        : null;
    
    [JsonIgnore]
    public DateTimeOffset? UpdatedAt => UpdatedTimestamp > 0 
        ? DateTimeOffset.FromUnixTimeMilliseconds(UpdatedTimestamp) 
        : null;
}