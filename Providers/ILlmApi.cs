using LocalEmbeddings.Models;

namespace LocalEmbeddings.Providers;


public interface ILlmApi: IDisposable
{
    Task<(string, Message[])> GetCompletion(Message[] messages);
}