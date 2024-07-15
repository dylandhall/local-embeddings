using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record Message(
    [property: JsonPropertyName("role")] string Role, 
    [property: JsonPropertyName("content")]string Content);