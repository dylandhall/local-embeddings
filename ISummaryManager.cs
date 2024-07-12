namespace LocalEmbeddings;

public interface ISummaryManager
{
    Task<string> GetSummary(string text);
    Task<string> GetSummaryOfMatches(string search, List<Hit> matches);
}

public class SummaryManager(ILlmApi llmApi) : ISummaryManager
{
    public async Task<string> GetSummaryOfMatches(string search, List<Hit> matches)
    {
        try
        {
            var messages = new Message[] { new("system", LlmPrompts.SystemMessageBeforeSummaryOfMatches), }
                .Concat(matches
                    .Select(m => new Message("user", m.Summary)))
                .Append(new("user", $"{LlmPrompts.UserPromptBeforeSummaryOfMatches}: {search}"))
                .ToArray();

            var (reply, _) = await llmApi.GetCompletion(messages);
            return reply;
        }
        catch (Exception e)
        {
            Console.WriteLine($"Cannot access llm server: {e.Message}");
            return string.Empty;
        }
    }

    public async Task<string> GetSummary(string text)
    {
        try
        {
            var messages = new Message[]
            {
                new("system", LlmPrompts.PromptToSummariseDocument),
                new("user", text.Replace("\n", " ").Replace("\r", ""))
            };
            var (reply, _) = await llmApi.GetCompletion(messages);
            return reply;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return string.Empty;
        }
    }
}