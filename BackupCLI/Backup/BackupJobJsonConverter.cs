using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.FileSystem;
using Quartz;

namespace BackupCLI.Backup;

public class BackupJobJsonConverter : JsonConverter<BackupJob>
{
    public override BackupJob Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        var root = document.RootElement;

        var backupJob = new BackupJob();

        // sources
        if (root.TryGetProperty("sources", out var s) && s.Deserialize<List<string>>(Options) is { Count: > 0 } sources)
        {
            if (sources.Where(s => !Directory.Exists(s)).ToList() is { Count: > 0 } invalidSources)
                throw new DirectoryNotFoundException($"Missing source directories: {{ {string.Join(", ", invalidSources)} }}");

            backupJob.Sources = sources.Select(FileSystemUtils.FromPath).ToList();
        } 
        else throw new JsonException("Sources list is missing or empty");

        // targets
        if (root.TryGetProperty("targets", out var t) && t.Deserialize<List<string>>(Options) is { Count: > 0 } targets)
        {
            backupJob.Targets = targets.Select(Directory.CreateDirectory).ToList();
        } 
        else throw new JsonException("Targets list is missing or empty");

        // ancestry check
        if (backupJob.Targets.Any(target 
                => backupJob.Sources.Any(source 
                    => FileSystemUtils.AreDirectAncestors(source, target))))
            throw new ArgumentException("Targets cannot be direct ancestors of sources (and vice versa).");

        //timing
        if (root.TryGetProperty("timing", out var timing) && timing.ValueKind == JsonValueKind.String)
        {
            List<string> parts = timing.ToString().Split(' ').ToList();

            // standard cron expression are incompatible with the quartz format, so we need to convert them
            if (parts.Count == 5) parts.Insert(0, "0");

            // day of week and day of month are mutually exclusive
            if (parts[3] != "?" && parts[5] != "?") parts[5] = "?";

            backupJob.Timing = new CronExpression(string.Join(' ', parts));
        }
        else throw new JsonException("Timing cron string is of invalid type");

        // retention
        if (root.TryGetProperty("retention", out var r) && r.Deserialize<BackupRetention>(Options) is { } retention)
        {
            backupJob.Retention = retention;
        }
        else backupJob.Retention = new BackupRetention();

        // method
        if (root.TryGetProperty("method", out var m) && m.Deserialize<BackupMethod>(Options) is var method)
        {
            backupJob.Method = method;
        }
        else backupJob.Method = BackupMethod.Full;

        return backupJob;
    }

    public override void Write(Utf8JsonWriter writer, BackupJob value, JsonSerializerOptions options)
        => throw new NotImplementedException();

    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(), new BackupJobJsonConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}