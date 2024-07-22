using System.Text;
using LocalEmbeddings.Helpers;
using LocalEmbeddings.Models;
using LocalEmbeddings.Providers;

namespace LocalEmbeddings.Managers;

public class SearchManager(IPersistentStateServices services)
    : BaseState(CurrentState.InitialSearch)
{
    public CurrentState InitialState { init => CurrentState = value; }

    private readonly IVectorDb _vectorDb = services.VectorDb;
    private readonly IDocumentFileDownloader _documentFileDownloader = services.DocumentFileDownloader;
    private readonly IConversationSessionManager _conversationSessionManager = services.ConversationSessionManager;
    private readonly ISummaryManager _summaryManager = services.SummaryManager;

    private const int NumberOfResultsPerPage = 8;

    private Query CurrentQuery { get; set; } = new(string.Empty, 0);

    private int? _selectedMatch;
    private List<IDocument> _topMatches = new();

    private List<IDocument> TopMatches
    {
        get => _topMatches;
        set
        {
            _topMatches = value;
            _selectedMatch = null;
            SummaryOfMatches =
                new Lazy<Task<string>>(() => _summaryManager.GetSummaryOfMatches(CurrentQuery.QueryText, TopMatches));
        }
    }
    private Lazy<Task<string>> SummaryOfMatches { get; set; } = new(() => Task.FromResult(string.Empty));

    public override async Task<IProgramStateManager> UpdateAndProcess()
    {
        var res = CurrentState switch
        {
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
        Console.WriteLine(res);
        return ToState(res);
    }

    private async Task<CurrentState> GetChatCompletion()
    {
        Console.WriteLine("Querying, please wait..");
        Console.WriteLine();
        var reply = await _conversationSessionManager.GetCompletionForCurrentConversation();
        ConsoleHelper.WriteMarkdown(reply);
        Console.WriteLine();
        return CurrentState.AskingQuestions;
    }
    private async Task<CurrentState> GetSummaryChatCompletion()
    {
        Console.WriteLine("Querying, please wait..");
        Console.WriteLine();
        var reply = await _conversationSessionManager.GetCompletionForCurrentConversation();
        ConsoleHelper.WriteMarkdown(reply);
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

        _conversationSessionManager.UpdateConversationWithDocumentQuestion(questionText, issueBody);

        return CurrentState.GettingChatCompletion;
    }

    private CurrentState AskAnotherQuestionAboutCurrentIssue()
    {
        Console.WriteLine("Ask another question or enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.Summary;

        _conversationSessionManager.UpdateConversationWithQuestion(questionText);

        return CurrentState.GettingChatCompletion;
    }

    private async Task<CurrentState> AskQuestionAboutSummary()
    {
        Console.WriteLine("Ask question or enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.SummarisedAllIssues;

        _conversationSessionManager.UpdateConversationWithSummaryQuestion(questionText, await SummaryOfMatches.Value);

        return CurrentState.GettingChatCompletionForSummary;
    }

    private CurrentState AskAnotherQuestionAboutSummary()
    {
        Console.WriteLine("Ask another question or enter to return:");
        var questionText = Console.ReadLine()??string.Empty;

        if (string.IsNullOrWhiteSpace(questionText)) return CurrentState.SummarisedAllIssues;

        _conversationSessionManager.UpdateConversationWithQuestion(questionText);

        return CurrentState.GettingChatCompletionForSummary;
    }

    
    private CurrentState ShowSummary()
    {
        var issue = TopMatches[_selectedMatch!.Value];
        ConsoleHelper.WriteMarkdown($"## {issue.Title}{Environment.NewLine}{Environment.NewLine}{issue.Summary}");

        var url = _documentFileDownloader.GetUrlForDocument(issue.Id);
        if (!string.IsNullOrWhiteSpace(url)) Console.WriteLine("Location: " + url + Environment.NewLine);
        
        var actionFromKey = ConsoleHelper.GetActionFromKey(new()
        {
            { ConsoleKey.Q, (ActionEnum.Question, "ask a question about the current issue") },
            { ConsoleKey.N, (ActionEnum.QuestionInNewConversation, "ask a question about the current issue in a new conversation") },
            { ConsoleKey.R, (ActionEnum.Related, "search for related issues") },
            { ConsoleKey.C, (ActionEnum.Return, "continue searching issues") },
        }, isDefaultAllowed: false);

        if (actionFromKey == ActionEnum.QuestionInNewConversation)
            _conversationSessionManager.ResetConversation();

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
        ConsoleHelper.WriteMarkdown(await SummaryOfMatches.Value);
        return CurrentState.SummarisedAllIssues;
    }

    private CurrentState ShowingSummaryOfAllIssues()
    {
        var action = ConsoleHelper.GetActionFromKey(new()
        {
            { ConsoleKey.Q, (ActionEnum.Question, "ask a question about the summary") },
            { ConsoleKey.N, (ActionEnum.QuestionInNewConversation, "ask a question about the summary in a new conversation") },
        }, isDefaultAllowed: true, "continue looking through the search results");

        if (action == ActionEnum.Default) return CurrentState.SearchResults;

        if (action == ActionEnum.QuestionInNewConversation)
            _conversationSessionManager.ResetConversation();

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
        var (numIssue, action) = ConsoleHelper.GetActionOrNumberFromKey(options, maxNum: TopMatches.Count, isDefaultAllowed: true, "to start a new search");

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
        TopMatches = await _vectorDb.Query(CurrentQuery.QueryText, CurrentQuery.Offset);
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
    private static void WriteIssuesMenu(List<IDocument> topMatches)
    {
        Console.WriteLine();

        var sb = new StringBuilder();

        sb.AppendLine("## Top matches:");

        foreach (var (match, index) in topMatches.Select((m, i) => (m, i)))
            sb.AppendLine($"{index + 1}: **{match.Id}**: {match.Title}{Environment.NewLine}");

        sb.AppendLine();
        ConsoleHelper.WriteMarkdown(sb.ToString());
    }




}