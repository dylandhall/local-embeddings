namespace LocalEmbeddings;

public record Prompts(
    string PromptToAnswerQuestionAboutDocument,
    string PromptToAnswerQuestionAboutSummary,
    string PromptToSummariseDocument,
    string SystemMessageBeforeAnsweringQuestions,
    string SystemMessageBeforeSummaryOfMatches,
    string UserPromptBeforeSummaryOfMatches) : BaseSettings<Prompts>
{
    public static Task<Prompts> ReadSettings() => BaseSettings<Prompts>.ReadSettings(
        filename: "prompts.json",
        getDefault: () => new Prompts(
            PromptToAnswerQuestionAboutDocument: "I'm going to give you document, and I need you to answer the following question",
            PromptToAnswerQuestionAboutSummary: "I'm going to give you set of documents, and I need you to answer the following question about them",
            PromptToSummariseDocument: "You are going to be given a document, which specifies a feature or describes a bug. You are required to summarise it for later searching. You need to include the names of the affected parts of the system and a short but detailed summary of either the changes requested, or the bug being reported. Try as hard as possible to include all detail without including extraneous or generic details.",
            SystemMessageBeforeAnsweringQuestions: "You are a helpful assistant who specialises in answering questions about design documents, which include details of features for software library. Answer the question as best you can with the details in the issue, as succinctly as possible, without adding anything you are unsure about",
            SystemMessageBeforeSummaryOfMatches: "You are a helpful assistant who searches through a database of documents for a user. The user will give you documents, then ask you a question, you will give a short summary to the user explaining how the issues relate to the user's search.",
            UserPromptBeforeSummaryOfMatches: "Please give a short summary of all of the above issues, with one bullet point per issue. Please also comment on how the issues relate to each other (particularly if they are bugs), and how they relate to this search"
        ),
        isValid: prompts => prompts is
        {
            PromptToAnswerQuestionAboutDocument: not null,
            PromptToAnswerQuestionAboutSummary: not null,
            PromptToSummariseDocument: not null,
            SystemMessageBeforeAnsweringQuestions: not null,
            SystemMessageBeforeSummaryOfMatches: not null,
            UserPromptBeforeSummaryOfMatches: not null
        });
}