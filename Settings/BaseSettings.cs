using System.Text.Json;

namespace LocalEmbeddings.Settings;

// i should just use dotnet appsettings but for some reason i always
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

            Console.WriteLine($"Settings for {typeof(T).Name} not found - writing new settings file to {settingsFile}.");

            var defaultValues = getDefault();
            await File.WriteAllTextAsync(settingsFile, JsonSerializer.Serialize(defaultValues));

            Console.WriteLine();
            Console.WriteLine($"{settingsFile} written.");
            Console.WriteLine("Please edit the file in a text editor to customise for future runs.");

            return defaultValues;
        }

        var settings = JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(settingsFile));

        if (settings is null || !isValid(settings)) throw new Exception($"Settings at {settingsFile} not valid");

        Console.WriteLine($"Loaded {typeof(T).Name} configuration from {settingsFile}");
        _settings = settings;
        return settings;
    }
}