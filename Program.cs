using LocalEmbeddings.Managers;
using LocalEmbeddings.Providers;
using LocalEmbeddings.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace LocalEmbeddings;

class Program
{
    static async Task Main(string[] args)
    {
        var doRefresh = args.Any(a => a.Contains("--refresh", StringComparison.OrdinalIgnoreCase));
        var reindexExisting = args.Any(a => a.Contains("--reindex", StringComparison.OrdinalIgnoreCase));
        var fullGithubRefresh = args.Any(a => a.Contains("--full-github-refresh", StringComparison.OrdinalIgnoreCase));

        
        if (!doRefresh && (reindexExisting || fullGithubRefresh))
        {
            Console.WriteLine("Refresh must be selected if reindexing or refreshing github");
            return;
        }
        
        if (!doRefresh)
        {
            Console.WriteLine(
                "Run with --refresh to refresh issues, summarise and index, add --reindex to reindex existing summaries");
            Console.WriteLine("and --full-github-refresh to check updates on all github issues");
            Console.WriteLine();
        }

        var services = new ServiceCollection();
        await ConfigureServices(services, doRefresh, reindexExisting, fullGithubRefresh);

        await using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var state = scope.ServiceProvider.GetRequiredService<IProgramStateManager>();

        while (state is not { CurrentState: CurrentState.Finished })
            await state.UpdateAndProcess();
    }

    private static async Task ConfigureServices(ServiceCollection services,bool doRefresh,
        bool reindexExisting, bool fullGithubRefresh)
    {
        var apiSettings = await ApiSettings.ReadSettings();
        var prompts = await Prompts.ReadSettings();
        var githubSettings = await GithubSettings.ReadSettings();

        services.AddSingleton(apiSettings);
        services.AddSingleton(githubSettings);
        services.AddSingleton(prompts);

        services.AddSingleton<IProgramSettings>(new ProgramSettings(doRefresh, reindexExisting, fullGithubRefresh));

        // these need to be disposed
        services.AddScoped<IVectorDb, MarqoDb>();
        services.AddScoped<ILlmApi, LlmApi>();
        services.AddScoped<IProgramStateManager, ProgramProgramStateManagerManager>();
        
        // transient, injection will provide a separate tracked conversation 
        services.AddTransient<IConversationSessionManager, ConversationSessionManager>();

        services.AddSingleton<ISummaryManager, SummaryManager>();
        services.AddSingleton<IDocumentFileDownloader, GithubIssueDownloader>();
    }
}

public interface IProgramSettings
{
    bool Refresh { get; }
    bool Reindex { get; }
    bool FullGithubRefresh { get; }
}

public record ProgramSettings(bool Refresh, bool Reindex, bool FullGithubRefresh) : IProgramSettings;