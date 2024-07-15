namespace LocalEmbeddings.Models;

public interface IDocument
{
    string Id { get; }
    string Content { get; }
    string Title { get; }
    string Summary { get; }
}