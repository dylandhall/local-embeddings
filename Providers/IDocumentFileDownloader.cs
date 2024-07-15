namespace LocalEmbeddings.Providers;

public interface IDocumentFileDownloader
{
    Task GetIssues(string folder);
    string GetUrlForDocument(string id);
    string GetFileName(string id);
    string GetSummaryFileName(string id);
    string FileNameMask { get; }
}