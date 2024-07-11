using System.Text.Json;

namespace LocalEmbeddings;

// i should just setup dotnet appsettings but for some reason i always
// just find rehydrating serialised objects easier to work with

public abstract record BaseSettings<T> where T : class
{
    private static T? _settings;
    protected static async Task<T> ReadSettings(string filename, Func<T> getDefault, Func<T?, bool> isValid)
    {
        if (_settings != default) return _settings;

        var settingsFile = Path.Join(AppContext.BaseDirectory, filename);
        if (!File.Exists(settingsFile))
        {
            await File.WriteAllTextAsync(settingsFile, JsonSerializer.Serialize(getDefault()));
            
            Console.WriteLine($"Settings not found, please update the settings file at {settingsFile} and run again");

            throw new Exception("Cannot find settings");
        }

        var settings = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(settingsFile));

        if (settings is null || !isValid(settings)) throw new Exception($"Settings at {settingsFile} not valid");

        _settings = settings;
        return settings;
    }
}