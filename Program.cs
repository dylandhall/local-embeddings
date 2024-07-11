using System.Text.Json.Serialization;
using LocalEmbeddings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

class Program
{
    static async Task Main(string[] args)
    {
        var doRefresh = args.Any(a => a.Contains("--refresh", StringComparison.OrdinalIgnoreCase));
        var reindexExisting = args.Any(a => a.Contains("--reindex", StringComparison.OrdinalIgnoreCase));

        
        if (!doRefresh && reindexExisting)
        {
            Console.WriteLine("Refresh must be selected if reindexing");
            return;
        }
        
        if (!doRefresh)
        {
            Console.WriteLine(
                "Run with --refresh to refresh issues, summarise and index, add --reindex to reindex existing summaries");
            Console.WriteLine();
        }

        var services = new ServiceCollection();
        await ConfigureServices(services, doRefresh, reindexExisting);

        await using var serviceProvider = services.BuildServiceProvider();
        using var scope = serviceProvider.CreateScope();
        var state = scope.ServiceProvider.GetRequiredService<IState>();

        while (state is not { CurrentState: CurrentState.Finished })
        {
            await state.UpdateAndProcess();
        }
    }

    private static async Task ConfigureServices(ServiceCollection services, bool doRefresh, bool reindexExisting)
    {
        var apiSettings = ApiSettings.ReadSettings();
        var githubSettings = GithubSettings.ReadSettings();

        services.AddSingleton(await apiSettings);
        services.AddSingleton(await githubSettings);

        services.AddSingleton<IProgramSettings>(new ProgramSettings(doRefresh, reindexExisting));
        services.AddScoped<IVectorDb, MarqoDb>();
        services.AddScoped<ILlmApi, LlmApi>();
        services.AddScoped<IState, State>();
        services.AddSingleton<IMarkdownFileDownloader, GithubIssueDownloader>();
    }
}

public interface IProgramSettings
{
    bool Refresh { get; }
    bool Reindex { get; }
}

public record ProgramSettings(bool Refresh, bool Reindex) : IProgramSettings;

