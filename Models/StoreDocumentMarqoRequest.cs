using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record StoreDocumentMarqoRequest(
    [property: JsonPropertyName("documents")]List<Document> Documents, 
    [property: JsonPropertyName("tensorFields")]List<string> TensorFields);