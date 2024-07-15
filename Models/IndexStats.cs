using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record IndexStats(
    [property: JsonPropertyName("numberOfDocuments")] int NumberOfDocuments,
    [property: JsonPropertyName("numberOfVectors")] int NumberOfVectors,
    [property: JsonPropertyName("backend")] Backend Backend
);