using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Helpers;

namespace BackupCLI.Backup;

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

    public static bool TryLoadFile(string path, out List<BackupJobJson>? jobs)
    {
        jobs = default;

        try
        {
            jobs = LoadFile(path);
            return true;
        }
        catch (Exception e)
        {
            Program.Logger.Error(e);
            return false;
        }
    }
}

public class BackupJobJson : ValidJson
{
    public List<string> Sources { get; set; } = [];
    public List<string> Targets { get; set; } = [];
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