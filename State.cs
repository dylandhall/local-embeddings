using System.Diagnostics;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ConsoleMarkdownRenderer;
using Dasync.Collections;

namespace LocalEmbeddings;

public interface IState 
{
    Task UpdateAndProcess();
    CurrentState CurrentState { get; }
}

public class State(
    ApiSettings apiSettings,
    IVectorDb vectorDb,
    IProgramSettings args,
    IMarkdownFileDownloader markdownFileDownloader,
    IConversationManager conversationManager,
    ISummaryManager summaryManager)
    : IState
{
    private const int NumberOfResultsPerPage = 8;
    public CurrentState CurrentState { get; private set; } = CurrentState.Starting;

    private readonly bool _refresh = args.Refresh;
    private readonly bool _reindex = args.Reindex;
    private Query CurrentQuery { get; set; } = new(string.Empty, 0);

    private int? _selectedMatch;
    private List<Hit> _topMatches = new();

    private List<Hit> TopMatches
    {
        get => _topMatches;
        set
        {
            _topMatches = value;
            _selectedMatch = null;
            SummaryOfMatches =
                new Lazy<Task<string>>(() => summaryManager.GetSummaryOfMatches(CurrentQuery.QueryText, TopMatches));
        }
    }
    private Lazy<Task<string>> SummaryOfMatches { get; set; } = new(() => Task.FromResult(string.Empty));

    public async Task UpdateAndProcess()
    {
        CurrentState = CurrentState switch
        {
            CurrentState.Starting => await Start(),
            CurrentState.Refreshing => await RefreshDatabase(),
            CurrentState.ShowStats => await ShowStats(),
            CurrentState.InitialSearch => InitialSearch(),
            CurrentState.Searching => await Search(),
            CurrentState.SearchResults => ShowSearchResults(),
            CurrentState.SummariseAllIssues => await SummariseIssues(),
            CurrentState.SummarisedAllIssues => ShowingSummaryOfAllIssues(),
            CurrentState.AskQuestionAboutSummary => await AskQuestionAboutSummary(),
            CurrentState.FindRelated => FindRelatedIssues(),
            CurrentState.Summary => ShowSummary(),
            CurrentState.AskQuestion => AskQuestionAboutCurrentIssue(),
            CurrentState.GettingChatCompletion => await GetChatCompletion(),
            CurrentState.GettingChatCompletionForSummary => await GetSummaryChatCompletion(),
            CurrentState.AskingQuestions => AskAnotherQuestionAboutCurrentIssue(),
            CurrentState.AskFollowOnQuestionAboutSummary => AskAnotherQuestionAboutSummary(),
            _ => throw new ArgumentOutOfRangeException()
        };

    }

    private async Task<CurrentState> GetChatCompletion()
    {
        Console.WriteLine("Querying, please wait..");
        Console.WriteLine();
        var reply = await conversationManager.GetCompletionForCurrentConversation();
        WriteMarkdown(reply);
        Console.WriteLine();
        return CurrentState.AskingQuestions;
    }
    private async Task<CurrentState> GetSummaryChatCompletion()
    {
        Console.WriteLine("Querying, please wait..");
        Console.WriteLine();
        var reply = await conversationManager.GetCompletionForCurrentConversation();
        WriteMarkdown(reply);
        Console.WriteLine();
        return CurrentState.AskFollowOnQuestionAboutSummary;
    }
    
    private CurrentState AskQuestionAboutCurrentIssue()
    {
        Console.WriteLine("Ask a question about this issue, enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.Summary;

        var selected = TopMatches[_selectedMatch!.Value];

        string issueBody = selected.Content;

        conversationManager.UpdateConversationWithNewIssueQuestion(questionText, issueBody);

        return CurrentState.GettingChatCompletion;
    }

    private CurrentState AskAnotherQuestionAboutCurrentIssue()
    {
        Console.WriteLine("Ask another question or enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.Summary;

        conversationManager.UpdateConversationWithQuestion(questionText);

        return CurrentState.GettingChatCompletion;
    }

    private async Task<CurrentState> AskQuestionAboutSummary()
    {
        Console.WriteLine("Ask question or enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.SummarisedAllIssues;

        conversationManager.UpdateConversationWithSummaryQuestion(questionText, await SummaryOfMatches.Value);

        return CurrentState.GettingChatCompletionForSummary;
    }

    private CurrentState AskAnotherQuestionAboutSummary()
    {
        Console.WriteLine("Ask another question or enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.SummarisedAllIssues;

        conversationManager.UpdateConversationWithQuestion(questionText);

        return CurrentState.GettingChatCompletionForSummary;
    }

    
    private CurrentState ShowSummary()
    {
        var issue = TopMatches[_selectedMatch!.Value];
        WriteMarkdown($"## {issue.Title}{Environment.NewLine}{Environment.NewLine}{issue.Summary}");

        var url = markdownFileDownloader.GetUrlForDocument(issue.Id);
        if (!string.IsNullOrWhiteSpace(url)) Console.WriteLine("Location: " + url + Environment.NewLine);
        
        var actionFromKey = GetActionFromKey(new()
        {
            { ConsoleKey.Q, (ActionEnum.Question, "ask a question about the current issue") },
            { ConsoleKey.N, (ActionEnum.QuestionInNewConversation, "ask a question about the current issue in a new conversation") },
            { ConsoleKey.R, (ActionEnum.Related, "search for related issues") },
            { ConsoleKey.C, (ActionEnum.Return, "continue searching issues") },
        }, isDefaultAllowed: false);

        if (actionFromKey == ActionEnum.QuestionInNewConversation)
            conversationManager.ResetConversation();

        return actionFromKey switch
            {
                ActionEnum.Question => CurrentState.AskQuestion,
                ActionEnum.QuestionInNewConversation => CurrentState.AskQuestion,
                ActionEnum.Related => CurrentState.FindRelated,
                ActionEnum.Return => CurrentState.SearchResults,
                _ => throw new ArgumentOutOfRangeException()
            };
    }

    private CurrentState FindRelatedIssues()
    {
        var searchIssue = TopMatches[_selectedMatch!.Value];
        CurrentQuery = new Query(searchIssue.Title + " " + searchIssue.Summary, 0);
        return CurrentState.Searching;
    }

    private async Task<CurrentState> SummariseIssues()
    {
        Console.WriteLine("Summarising, please wait..");
        WriteMarkdown(await SummaryOfMatches.Value);
        return CurrentState.SummarisedAllIssues;
    }

    private CurrentState ShowingSummaryOfAllIssues()
    {
        var action = GetActionFromKey(new()
        {
            { ConsoleKey.Q, (ActionEnum.Question, "ask a question about the summary") },
            { ConsoleKey.N, (ActionEnum.QuestionInNewConversation, "ask a question about the summary in a new conversation") },
        }, isDefaultAllowed: true, "continue looking through the search results");

        if (action == ActionEnum.Default) return CurrentState.SearchResults;

        if (action == ActionEnum.QuestionInNewConversation)
            conversationManager.ResetConversation();

        return CurrentState.AskQuestionAboutSummary;
    }

    private CurrentState ShowSearchResults()
    {
        WriteIssuesMenu(TopMatches);

        if (CurrentQuery.Offset>0)
            Console.WriteLine($"Page {(CurrentQuery.Offset / NumberOfResultsPerPage) + 1}");

        var options = new Dictionary<ConsoleKey, (ActionEnum action, string description)>()
        {
            { ConsoleKey.N, (ActionEnum.NextPage, "view the next page") },
            { ConsoleKey.S, (ActionEnum.SummariseIssues, "display a summary of these issues") },
        };
        if (CurrentQuery.Offset > 0)
        {
            options.Add(ConsoleKey.P, (ActionEnum.PreviousPage, "go back to the previous page"));
        }
        var (numIssue, action) = GetActionOrNumberFromKey(options, maxNum: TopMatches.Count, isDefaultAllowed: true, "to start a new search");

        if (numIssue.HasValue)
        {
            _selectedMatch = numIssue;
            return CurrentState.Summary;
        }

        switch (action)
        {
            case ActionEnum.SummariseIssues:
                return CurrentState.SummariseAllIssues;
            case ActionEnum.Default:
                return CurrentState.InitialSearch;
            case ActionEnum.NextPage:
                CurrentQuery = CurrentQuery with { Offset = CurrentQuery.Offset + NumberOfResultsPerPage };
                return CurrentState.Searching;
            default:
                CurrentQuery = CurrentQuery with { Offset = Math.Min(0, CurrentQuery.Offset - NumberOfResultsPerPage) };
                return CurrentState.Searching;
        }
    }

    private async Task<CurrentState> Search()
    {
        Console.WriteLine();
        Console.WriteLine("Searching, please wait..");
        TopMatches = await vectorDb.Query(CurrentQuery.QueryText, CurrentQuery.Offset);
        Console.WriteLine();
        return TopMatches.Count > 0
            ? CurrentState.SearchResults
            : CurrentState.InitialSearch;
    }

    private CurrentState InitialSearch()
    {
        Console.WriteLine("Search the issue database or hit enter to close:");
        var queryText = Console.ReadLine() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(queryText))
            return CurrentState.Finished;

        CurrentQuery = new Query(queryText, Offset: 0);
        return CurrentState.Searching;
    }

    private async Task<CurrentState> ShowStats()
    {
        var stats = await vectorDb.GetIndexStats();

        if (stats is { Backend: not null })
        {
            Console.WriteLine("Marqo server status:");
            Console.WriteLine(
                $"Backend: Memory {Math.Round(stats.Backend.MemoryUsedPercentage, 2)}%, Storage {Math.Round(stats.Backend.StorageUsedPercentage, 2)}%");
            Console.WriteLine();
        }

        var indexes = await vectorDb.GetIndexList();

        Console.WriteLine("Current Marqo indexes:");
        foreach (var index in indexes)
            Console.WriteLine(index);
        Console.WriteLine();
        Console.WriteLine($"Current index: {apiSettings.MarqoIndex}");
        if (stats is not null)
            Console.WriteLine($"Documents: {stats.NumberOfDocuments}, Vectors: {stats.NumberOfVectors}");
        Console.WriteLine();

        return CurrentState.InitialSearch;
    }

    private async Task<CurrentState> RefreshDatabase()
    {
        Console.WriteLine("Syncing issues from github");
        await markdownFileDownloader.GetIssues(apiSettings.LocalFolder);

        var files = Directory.GetFiles(apiSettings.LocalFolder, "*.markdown");

        var documents = await GetFileDataAndSaveSummary(files, apiSettings)
            .Select(v => new Document(Path.GetFileNameWithoutExtension(v.File), v.File, v.Content, v.Title, v.Summary))
            .ToListAsync();

        await vectorDb.StoreEmbeddings(documents, _reindex);

        return CurrentState.InitialSearch;
    }

    private async Task<CurrentState> Start()
    {
        await vectorDb.InitializeIndex();

        return _refresh
            ? CurrentState.Refreshing
            : CurrentState.ShowStats;
    }

    private async IAsyncEnumerable<FileSummary> GetFileDataAndSaveSummary(IEnumerable<string> files,
        ApiSettings apiSettings, int llmConcurrency = 1)
    {
        const int maxConcurrency = 32;
        var llmSemaphore = new SemaphoreSlim(llmConcurrency);
        var tasks = new List<Task<FileSummary>>();

        foreach (var file in files.Where(f => !string.IsNullOrWhiteSpace(f)))
        {
            if (tasks.Count >= maxConcurrency)
            {
                var completedTask = await Task.WhenAny(tasks);
                tasks.Remove(completedTask);
                yield return await completedTask;
            }

            tasks.Add(ProcessFile(file, llmSemaphore, apiSettings));
        }

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            yield return await completedTask;
        }
    }

    private async Task<FileSummary> ProcessFile(string file, SemaphoreSlim semaphore, ApiSettings apiSettings)
    {
        var sw = new Stopwatch();
        string content = await File.ReadAllTextAsync(file);
        Regex.Replace(content, "\\(data:image/\\w+;base64,[^\\)]+\\)", "()");
        string title = content.Split(Environment.NewLine).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim(' ')
            .Trim('#').Trim() ?? "";

        var summaryFile = $"{file}.summary";

        if (File.Exists(summaryFile) && File.GetLastWriteTimeUtc(summaryFile) >= File.GetLastWriteTimeUtc(file))
        {
            string summary = await File.ReadAllTextAsync(summaryFile);
            return new(file, content, title, summary);
        }

        await semaphore.WaitAsync();

        sw.Start();
        Console.WriteLine("Creating summary for " + file);
        try
        {
            var summary = await summaryManager.GetSummary(content);

            await File.WriteAllTextAsync(summaryFile, summary);

            sw.Stop();
            Console.WriteLine($"Created summary for {file} in {sw.Elapsed}");
            return new(file, content, title, summary);
        }
        finally
        {
            sw.Stop();
            sw.Reset();
            semaphore.Release();
        }
    }

    private static void WriteMarkdown(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return;

        markdown = markdown.Replace("\r", "");
        markdown = Regex.Replace(markdown, "(?<!\n)\n(?!\n)", "\n\n\n");

        Displayer.DisplayMarkdown(markdown, new Uri(AppContext.BaseDirectory, UriKind.Absolute),
            allowFollowingLinks: false);
    }

    private record FileSummary(string File, string Content, string Title, string Summary);

    private static void WriteIssuesMenu(List<Hit> topMatches)
    {
        Console.WriteLine();

        var sb = new StringBuilder();

        sb.AppendLine("## Top matches:");

        foreach (var (match, index) in topMatches.Select((m, i) => (m, i)))
            sb.AppendLine($"{index + 1}: **{match.Id}**: {match.Title}{Environment.NewLine}");

        sb.AppendLine();
        WriteMarkdown(sb.ToString());
    }

    private static (int? numberSelected, ActionEnum? action) GetActionOrNumberFromKey(
        Dictionary<ConsoleKey, (ActionEnum action, string description)> actions, int maxNum, bool isDefaultAllowed,
        string? defaultText = "")
    {
        var sb = new StringBuilder();

        foreach (var action in actions)
        {
            sb.AppendLine($"* **Press {action.Key}** to {action.Value.description}");
        }

        if (isDefaultAllowed)
        {
            sb.AppendLine($"Any other key to {defaultText}");
        }

        sb.AppendLine();
        WriteMarkdown(sb.ToString());

        if (isDefaultAllowed)
        {
            var key = Console.ReadKey(intercept: true);

            return GetReturn(actions, key, maxNum);
        }

        {
            ConsoleKeyInfo? key = null;
            while (key == null || (!actions.ContainsKey(key.Value.Key) &&
                                   !(key.Value.KeyChar >= '1' && key.Value.KeyChar < maxNum + '1')))
            {
                key = Console.ReadKey(intercept: true);
            }

            return GetReturn(actions, key.Value, maxNum);
        }

        static (int? numberSelected, ActionEnum? action) GetReturn(
            Dictionary<ConsoleKey, (ActionEnum action, string description)> actions, ConsoleKeyInfo key, int maxNum)
        {
            return actions.TryGetValue(key.Key, out var action)
                ? (null, action.action)
                : key.KeyChar >= '1' && key.KeyChar < maxNum + '1'
                    ? (key.KeyChar - '1', null)
                    : (null, ActionEnum.Default);
        }
    }

    private static ActionEnum GetActionFromKey(Dictionary<ConsoleKey, (ActionEnum action, string description)> actions,
        bool isDefaultAllowed, string? defaultText = "")
    {
        var sb = new StringBuilder();

        foreach (var action in actions)
        {
            sb.AppendLine($"* **Press {action.Key}** to {action.Value.description}");
        }

        if (isDefaultAllowed)
        {
            sb.AppendLine($"Any other key to {defaultText}");
        }

        sb.AppendLine();
        WriteMarkdown(sb.ToString());

        if (isDefaultAllowed)
        {
            var key = Console.ReadKey(intercept:true).Key;

            return actions.TryGetValue(key, out var action)
                ? action.action
                : ActionEnum.Default;
        }

        {
            ConsoleKey? key = null;
            while (key == null || !actions.ContainsKey(key.Value))
            {
                key = Console.ReadKey(intercept: true).Key;
            }

            return actions.TryGetValue(key.Value, out var action)
                ? action.action
                : ActionEnum.Default;
        }
    }


}

public enum ActionEnum
{
    NextPage,
    PreviousPage,
    SummariseIssues,
    Return,
    Question,
    Default,
    Related,
    QuestionInNewConversation
}
public enum CurrentState
{
    Starting,
    Refreshing,
    ShowStats,
    InitialSearch,
    SearchResults,
    Summary,
    SummariseAllIssues,
    AskQuestion,
    AskingQuestions,
    Finished,
    Searching,
    FindRelated,
    GettingChatCompletion,
    SummarisedAllIssues,
    AskQuestionAboutSummary,
    GettingChatCompletionForSummary,
    AskFollowOnQuestionAboutSummary
}
public record Query(string QueryText, int Offset);


public record Message(
    [property: JsonPropertyName("role")] string Role, 
    [property: JsonPropertyName("content")]string Content);