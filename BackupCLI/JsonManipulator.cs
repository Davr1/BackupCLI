using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using Quartz;

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
        if (JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(path), Options) is not { } backupJobs)
            throw new JsonException("Input file is not a valid JSON file.");

        backupJobs.ForEach(job => job.Validate());

        foreach (var job in backupJobs)
        {
            foreach (var s in job.Sources.Where(source => !Path.Exists(source)))
                throw new DirectoryNotFoundException($"Source directory {s} does not exist.");
        }

        return backupJobs;
    }
}

public class BackupJob : ValidJson
{
    public List<string> Sources { get; set; } = new();
    public List<string> Targets { get; set; } = new();
    public CronExpression? Cron { get; set; }
    public string? Timing
    {
        get => Cron?.CronExpressionString;
        set
        {
            if (value is null) return;

            List<string> parts = value.Split(' ').ToList();

            if (parts.Count == 5)
            {
                parts.Insert(0, "0");
                if (parts[3] != "?" && parts[5] != "?") parts[5] = "?";
            }
            Cron = new CronExpression(string.Join(' ', parts));
        }
    }

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
    private Dictionary<string, object?> DefaultProps { get; }

    private object? PropOrDefault(PropertyInfo prop) => prop.GetValue(this) ?? DefaultProps[prop.Name];

    public void Validate()
    {
        foreach (var prop in GetType().GetProperties())
            prop.SetValue(this, PropOrDefault(prop) ?? throw new JsonException($"{prop.Name} cannot be null."));
    }

    protected ValidJson()
    {
        DefaultProps = GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(this));
    }
}