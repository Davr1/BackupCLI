using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackupCLI;

public static class JsonManipulator
{
    private static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static List<BackupJob> LoadFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Input file not found.");

        string json = File.ReadAllText(path);

        if (JsonSerializer.Deserialize<List<BackupJob>>(json, Options) is not { } backupJobs)
            throw new JsonException("Input file is not a valid JSON file.");

        backupJobs.ForEach(job => job.Validate());

        foreach (var job in backupJobs)
        {
            job.Sources = job.Sources.Where(Path.Exists).ToList();
            job.Targets = job.Targets.Where(Path.Exists).ToList();
        }

        return backupJobs;
    }
}

public class BackupJob : ValidJson
{
    public List<string> Sources { get; set; } = new();
    public List<string> Targets { get; set; } = new();
    public string Timing { get; set; } = null!;
    public BackupRetention Retention { get; set; } = new();
    public BackupMethod Method { get; set; } = BackupMethod.Full;
}

public class BackupRetention : ValidJson
{
    public int Count { get; set; } = 2;
    public int Size { get; set; } = 0;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupMethod
{
    Full,
    Differential,
    Incremental
}

public class ValidJson
{
    private Dictionary<string, object?> DefaultProps { get; } = new();

    public void Validate()
    {
        foreach (var prop in GetType().GetProperties().Where(prop => prop.GetValue(this) is null))
        {
            var value = DefaultProps[prop.Name] ?? throw new JsonException($"{prop.Name} cannot be null.");
            prop.SetValue(this, value);
        }
    }

    protected ValidJson()
    {
        foreach (var prop in GetType().GetProperties())
            DefaultProps.Add(prop.Name, prop.GetValue(this));
    }
}