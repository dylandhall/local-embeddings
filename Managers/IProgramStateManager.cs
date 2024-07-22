namespace LocalEmbeddings.Managers;

public interface IProgramStateManager 
{
    Task<IProgramStateManager> UpdateAndProcess(); 
    bool IsFinished { get; }
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
    QuestionInNewConversation,
    BuildSystemSummaryPrompt,
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
    AskFollowOnQuestionAboutSummary,
    BuildingSystemSummaryPrompt
}
public record Query(string QueryText, int Offset);