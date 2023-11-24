using System.Reflection;
using System.Runtime.CompilerServices;
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

        foreach (var job in backupJobs)
        {
            job.Validate();

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

    [JsonIgnore]
    public CronExpression? Cron { get; set; }

    public string? Timing
    {
        get => Cron?.CronExpressionString;
        set
        {
            if (value is null) return;

            List<string> parts = value.Split(' ').ToList();

            // standard cron expression are incompatible with the quartz format, so we need to convert them
            if (parts.Count == 5)
            {
                // seconds
                parts.Insert(0, "0");
                // day of week and day of month are mutually exclusive
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

/// <summary>
/// Provides non-null type checking and default value support for deserialization using the default JSON library. 
/// </summary>
public class ValidJson
{
    private Dictionary<string, object?> DefaultProps { get; }

    private object? PropOrDefault(PropertyInfo prop) => prop.GetValue(this) ?? DefaultProps[prop.Name];

    public void Validate()
    {
        PropertyInfo[] props = GetType().GetProperties();
        MethodInfo validate = GetType().GetMethod("Validate")!;

        foreach (var prop in props.Where(p => p.GetCustomAttribute<JsonIgnoreAttribute>() is null))
            prop.SetValue(this, PropOrDefault(prop) ?? throw new JsonException($"{prop.Name} cannot be null."));

        foreach (var prop in props.Where(p => typeof(ValidJson).IsAssignableFrom(p.PropertyType)))
            validate.Invoke(prop.GetValue(this), null);
    }

    protected ValidJson()
    {
        DefaultProps = GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(this));
    }
}