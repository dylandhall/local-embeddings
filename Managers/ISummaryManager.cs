using LocalEmbeddings.Models;
using LocalEmbeddings.Providers;
using LocalEmbeddings.Settings;

namespace LocalEmbeddings.Managers;

public interface ISummaryManager
{
    Task<string> GetSummary(string text);
    Task<string> GetSummaryOfMatches(string search, List<IDocument> matches);
}

public class SummaryManager(ILlmApi llmApi, Prompts prompts) : ISummaryManager
{
    public async Task<string> GetSummaryOfMatches(string search, List<IDocument> matches)
    {
        try
        {
            var messages = new Message[] { new("system", prompts.SystemMessageBeforeSummaryOfMatches), }
                .Concat(matches
                    .Select(m => new Message("user", m.Summary)))
                .Append(new("user", $"{prompts.UserPromptBeforeSummaryOfMatches}: {search}"))
                .ToArray();

            var (reply, _) = await llmApi.GetCompletion(messages);
            return reply;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }

    public async Task<string> GetSummary(string text)
    {
        try
        {
            var messages = new Message[]
            {
                new("system", prompts.PromptToSummariseDocument),
                new("user", text.Replace("\n", " ").Replace("\r", ""))
            };
            var (reply, _) = await llmApi.GetCompletion(messages);
            return reply;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return string.Empty;
        }
    }
}