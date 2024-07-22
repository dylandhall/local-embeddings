using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Dasync.Collections;
using LocalEmbeddings.Helpers;
using LocalEmbeddings.Models;
using LocalEmbeddings.Providers;

namespace LocalEmbeddings.Managers;

public class IndexingManager(IPersistentStateServices services) : BaseState(CurrentState.Starting)
{
    private readonly bool _refresh = services.Args.Refresh;
    private readonly bool _reindex = services.Args.Reindex;
    public override async Task<IProgramStateManager> UpdateAndProcess() =>
        CurrentState switch
        {
            CurrentState.Starting => await Start(),
            CurrentState.Refreshing => await RefreshDatabase(),
            CurrentState.ShowStats => await ShowStats(),
            CurrentState.BuildingSystemSummaryPrompt => await BuildSystemSummaryPrompt(),
            _ => throw new ArgumentOutOfRangeException()
        };

    private async Task<IProgramStateManager> BuildSystemSummaryPrompt()
    {
        //services.VectorDb.Query()
        throw new NotImplementedException();
    }

    private async Task<IProgramStateManager> Start()
    {
        await services.VectorDb.InitializeIndex();

        if (_refresh) return ToState(CurrentState.Refreshing);  

        if (Directory.Exists(services.ApiSettings.LocalFolder) &&
            Directory.GetFiles(services.ApiSettings.LocalFolder, services.DocumentFileDownloader.FileNameMask).Length > 0)
            return ToState(CurrentState.ShowStats);

        Console.WriteLine($"File library not found at {services.ApiSettings.LocalFolder} and refresh not selected , refresh y/[N]?");

        var res = Console.ReadKey();
        return res.Key != ConsoleKey.Y 
            ? ToState(CurrentState.Finished) 
            : ToState(CurrentState.Refreshing);
    }
    
    
    private async Task<SearchManager> ShowStats()
    {
        var stats = await services.VectorDb.GetIndexStats();

        if (stats is { Backend: not null })
        {
            Console.WriteLine("Marqo server status:");
            Console.WriteLine(
                $"Backend: Memory {Math.Round(stats.Backend.MemoryUsedPercentage, 2)}%, Storage {Math.Round(stats.Backend.StorageUsedPercentage, 2)}%");
            Console.WriteLine();
        }

        var indexes = await services.VectorDb.GetIndexList();

        Console.WriteLine("Current Marqo indexes:");
        foreach (var index in indexes)
            Console.WriteLine(index);
        Console.WriteLine();
        Console.WriteLine($"Current index: {services.ApiSettings.DbIndex}");
        if (stats is not null)
            Console.WriteLine($"Documents: {stats.NumberOfDocuments}, Vectors: {stats.NumberOfVectors}");
        Console.WriteLine();

        return new SearchManager(services);
    }


    private async Task<IProgramStateManager> RefreshDatabase()
    {
        Console.WriteLine("Syncing issues from github");
        await services.DocumentFileDownloader.GetIssues(services.ApiSettings.LocalFolder);

        var files = Directory.GetFiles(services.ApiSettings.LocalFolder, services.DocumentFileDownloader.FileNameMask);

        var documents = await GetFileDataAndSaveSummary(files)
            .Select(v => new Document(Path.GetFileNameWithoutExtension(v.File), 
                v.File, v.Content, v.Title, v.Summary, v.CreatedAt?.ToUnixTimeMilliseconds(), v.UpdatedAt?.ToUnixTimeMilliseconds(), v.ClosedAt?.ToUnixTimeMilliseconds()))
            .ToListAsync();

        await services.VectorDb.StoreEmbeddings(documents, _reindex);

        var res = ConsoleHelper.GetActionFromKey(
            new() { { ConsoleKey.Y, (ActionEnum.BuildSystemSummaryPrompt, "Build global system summary prompt? (this will take some time)") } },
            true, "Continue");

        return res == ActionEnum.Default
            ? new SearchManager(services)
            : ToState(CurrentState.BuildingSystemSummaryPrompt);
    }

    private async IAsyncEnumerable<FileSummary> GetFileDataAndSaveSummary(IEnumerable<string> files, int llmConcurrency = 1)
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

            tasks.Add(ProcessFile(file, llmSemaphore));
        }

        while (tasks.Count > 0)
        {
            var completedTask = await Task.WhenAny(tasks);
            tasks.Remove(completedTask);
            yield return await completedTask;
        }
    }
    
    

    private async Task<FileSummary> ProcessFile(string file, SemaphoreSlim semaphore)
    {
        var sw = new Stopwatch();
        string content = await File.ReadAllTextAsync(file);
        Regex.Replace(content, "\\(data:image/\\w+;base64,[^\\)]+\\)", "()");
        string title = content.Split(Environment.NewLine).FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim(' ')
            .Trim('#').Trim() ?? "";

        var summaryFile = services.DocumentFileDownloader.GetSummaryFileName(file);
        var metadataFile = services.DocumentFileDownloader.GetMetadataFilename(file);

        var metadata = File.Exists(metadataFile)
            ? JsonSerializer.Deserialize<Metadata>(await File.ReadAllTextAsync(metadataFile))
            : null;

        if (File.Exists(summaryFile) && File.GetLastWriteTimeUtc(summaryFile) >= File.GetLastWriteTimeUtc(file))
        {
            string summary = await File.ReadAllTextAsync(summaryFile);
            return new(file, content, title, summary, metadata?.CreatedAt, metadata?.UpdatedAt, metadata?.ClosedAt);
        }

        await semaphore.WaitAsync();

        sw.Start();
        Console.WriteLine("Creating summary for " + file);
        try
        {
            var summary = await services.SummaryManager.GetSummary(content);

            await File.WriteAllTextAsync(summaryFile, summary);

            sw.Stop();
            Console.WriteLine($"Created summary for {file} in {sw.Elapsed}");
            return new(file, content, title, summary, metadata?.CreatedAt, metadata?.UpdatedAt, metadata?.ClosedAt);
        }
        finally
        {
            sw.Stop();
            sw.Reset();
            semaphore.Release();
        }
    }


    private record FileSummary(string File, string Content, string Title, string Summary, DateTimeOffset? CreatedAt, DateTimeOffset? UpdatedAt, DateTimeOffset? ClosedAt);

    
}