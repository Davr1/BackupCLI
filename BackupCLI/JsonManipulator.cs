using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackupCLI;

public static class JsonManipulator
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static List<BackupJobJson> LoadFile(string path)
    {
        if (JsonSerializer.Deserialize<List<BackupJobJson>>(File.ReadAllText(path), Options) is not { } backupJobs)
            throw new JsonException("Input file is not a valid JSON file.");

        foreach (var job in backupJobs) job.Validate();

        return backupJobs;
    }
}

public class BackupJobJson : ValidJson
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
    public int Size { get; set; } = 1;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupMethod
{
    Full,
    Differential,
    Incremental
}