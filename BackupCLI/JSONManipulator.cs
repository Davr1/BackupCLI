using System.Text.Json;
using System.Text.Json.Serialization;

namespace BackupCLI;

public static class JSONManipulator
{
    public static List<BackupJob> LoadFile(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Input file not found");

        var backupJobs = JsonSerializer.Deserialize<List<BackupJob>>(File.ReadAllText(path));

        if (backupJobs is null) throw new JsonException("Input file is not a valid JSON file");

        foreach (var job in backupJobs)
        {
            job.Sources = job.Sources.Where(Path.Exists).ToList();
            job.Targets = job.Targets.Where(Path.Exists).ToList();
        }

        return backupJobs;
    }
}

public class BackupJob
{
    [JsonPropertyName("sources")]
    public List<string> Sources { get; set; }

    [JsonPropertyName("targets")]
    public List<string> Targets { get; set; }

    [JsonPropertyName("timing")]
    public string Timing { get; set; }

    [JsonPropertyName("retention")]
    public BackupRetention Retention { get; set; }

    [JsonPropertyName("method")]
    public BackupMethod Method { get; set; }
}

public class BackupRetention
{
    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BackupMethod
{
    Full,
    Differential,
    Incremental
}