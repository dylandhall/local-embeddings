namespace LocalEmbeddings.Models;

public record ChatCompletion(
    string Id,
    string Object,
    long Created,
    string Model,
    List<Choice> Choices,
    Usage Usage);