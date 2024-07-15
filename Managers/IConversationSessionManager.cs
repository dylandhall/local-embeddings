using LocalEmbeddings.Models;
using LocalEmbeddings.Providers;
using LocalEmbeddings.Settings;

namespace LocalEmbeddings.Managers;

public interface IConversationSessionManager
{
    void UpdateConversationWithDocumentQuestion(string question, string issueBody);
    void UpdateConversationWithSummaryQuestion(string question, string issueBody);
    void UpdateConversationWithQuestion(string content);
    void ResetConversation();
    Task<string> GetCompletionForCurrentConversation();
}

public class ConversationSessionManager(ILlmApi llmApi, Prompts prompts) : IConversationSessionManager
{
    public async Task<string> GetCompletionForCurrentConversation()
    {
        try
        {
            (var reply, Conversation) = await llmApi.GetCompletion(Conversation!);
            return reply;
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error getting completion: " + ex.Message);
            return string.Empty;
        }
    }
    
    public void UpdateConversationWithDocumentQuestion(string question, string issueBody)
    {
        var content = $"{prompts.PromptToAnswerQuestionAboutDocument}: {question}\n Document: {issueBody.Replace("\n", " ").Replace("\r", "")}";
        UpdateConversationWithQuestion(content);
    }

    public void UpdateConversationWithSummaryQuestion(string question, string issueBody)
    {
        var content = $"{prompts.PromptToAnswerQuestionAboutSummary}: {question}\n Documents: {issueBody.Replace("\n", " ").Replace("\r", "")}";
        UpdateConversationWithQuestion(content);
    }

    public void  UpdateConversationWithQuestion(string content)
    {
        Conversation = Conversation!
            .Append(new Message("user", content))
            .ToArray();
    }

    public void ResetConversation() => Conversation = null;

    private Message[]? _conversation;

    private Message[]? Conversation
    {
        get => _conversation ??= GetInitialQuestionMessage();
        set => _conversation = value;
    }
    private Message[] GetInitialQuestionMessage()
    {
        return [
            new Message("system", prompts.SystemMessageBeforeAnsweringQuestions),
        ];
    }
}