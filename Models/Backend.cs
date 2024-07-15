using System.Text.Json.Serialization;

namespace LocalEmbeddings.Models;

public record Backend(
    [property: JsonPropertyName("memoryUsedPercentage")] double MemoryUsedPercentage,
    [property: JsonPropertyName("storageUsedPercentage")] double StorageUsedPercentage
);