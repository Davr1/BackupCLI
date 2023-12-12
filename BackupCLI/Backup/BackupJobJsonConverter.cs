using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.FileSystem;
using BackupCLI.Helpers;
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
        if (root.DeserializeOrDefault<List<string>>("sources", options: Options) is { Count: > 0 } sources)
        {
            if (sources.Where(s => !Directory.Exists(s)).ToList() is { Count: > 0 } invalidSources)
                throw new DirectoryNotFoundException($"Missing source directories: {{ {string.Join(", ", invalidSources)} }}");

            backupJob.Sources = sources.Select(FileSystemUtils.FromPath).ToList();
        } 
        else throw new JsonException("Sources list is missing or empty");

        // targets
        if (root.DeserializeOrDefault<List<string>>("targets", options: Options) is { Count: > 0 } targets)
            backupJob.Targets = targets.Select(Directory.CreateDirectory).ToList();
        else
            throw new JsonException("Targets list is missing or empty");

        // ancestry check
        if (backupJob.Targets.Any(target => backupJob.Sources.Any(source => FileSystemUtils.AreDirectAncestors(source, target))))
            throw new ArgumentException("Targets cannot be direct ancestors of sources (and vice versa).");

        //timing
        backupJob.Timing = root.DeserializeOrDefault<CronExpression>("timing", options: Options)!;

        // retention
        backupJob.Retention = root.DeserializeOrDefault<BackupRetention>("retention", new(), Options)!;

        // method
        backupJob.Method = root.DeserializeOrDefault<BackupMethod>("method", options: Options);

        if (backupJob.Method == BackupMethod.Full)
            backupJob.Retention.Size = 1;

        return backupJob;
    }

    public override void Write(Utf8JsonWriter writer, BackupJob value, JsonSerializerOptions options)
        => throw new NotImplementedException();

    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(), new BackupJobJsonConverter(), new CronConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

public class CronConverter : JsonConverter<CronExpression>
{
    public override CronExpression Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException("Cron expression must be a string");

        List<string> parts = reader.GetString()!.Split(' ').ToList();

        if (parts.Count is < 5 or > 7)
            throw new JsonException("Invalid cron expression");

        // standard cron expression are incompatible with the quartz format, so we need to convert them
        if (parts.Count == 5) parts.Insert(0, "0");

        // day of week and day of month are mutually exclusive
        if (parts[3].Contains('*') && parts[5] != "?") parts[3] = "?";
        else if (parts[5].Contains('*') && parts[3] != "?") parts[5] = "?";

        return new CronExpression(string.Join(' ', parts));
    }

    public override void Write(Utf8JsonWriter writer, CronExpression value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.CronExpressionString);
}
