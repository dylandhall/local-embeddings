using LocalEmbeddings.Managers;
using LocalEmbeddings.Settings;

namespace LocalEmbeddings.Providers;

public interface IPersistentStateServices
{
    ApiSettings ApiSettings {get;}
    IVectorDb VectorDb {get;}
    IProgramSettings Args {get;}
    IDocumentFileDownloader DocumentFileDownloader {get;}
    IConversationSessionManager ConversationSessionManager {get;}
    ISummaryManager SummaryManager {get;}
}
public class PersistentStateServices(
    ApiSettings apiSettings,
    IVectorDb vectorDb,
    IProgramSettings args,
    IDocumentFileDownloader documentFileDownloader,
    IConversationSessionManager conversationSessionManager,
    ISummaryManager summaryManager)
    : IPersistentStateServices
{
    public ApiSettings ApiSettings { get; } = apiSettings;
    public IVectorDb VectorDb { get; } = vectorDb;
    public IProgramSettings Args { get; } = args;
    public IDocumentFileDownloader DocumentFileDownloader { get; } = documentFileDownloader;
    public IConversationSessionManager ConversationSessionManager { get; } = conversationSessionManager;
    public ISummaryManager SummaryManager { get; } = summaryManager;
}
