using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

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