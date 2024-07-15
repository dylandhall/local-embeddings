using LocalEmbeddings.Models;

namespace LocalEmbeddings.Providers;

public interface IVectorDb: IDisposable
{
    Task StoreEmbeddings(List<Document> allDocuments, bool reindexExisting);
    Task<List<IDocument>> Query(string query, int offset);
    Task InitializeIndex();
    Task<IndexStats?> GetIndexStats();
    Task<string[]> GetIndexList();
}