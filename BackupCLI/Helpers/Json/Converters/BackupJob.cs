using BackupCLI.Backup;
using Quartz;
using System.Text.Json.Serialization;
using System.Text.Json;
using BackupCLI.Helpers.Extensions;
using BackupCLI.Helpers.FileSystem;

namespace BackupCLI.Helpers.Json.Converters;

/// <summary>
/// Provides a custom parser for <see cref="BackupJob"/> json files while also checking the validity of the input.
/// </summary>
public class BackupJobConverter : JsonConverter<BackupJob>
{
    public override BackupJob Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        var root = document.RootElement;

        var backupJob = new BackupJob();

        // sources
        if (root.DeserializeOrDefault<List<string>>("sources", options: options) is { Count: > 0 } sources)
        {
            if (sources.Where(s => !Directory.Exists(s)).ToList() is { Count: > 0 } invalidSources)
                throw new DirectoryNotFoundException($"Missing source directories: {{ {string.Join(", ", invalidSources)} }}");

            backupJob.Sources = sources.Select(FileSystemUtils.FromPath).ToList();
        }
        else throw new JsonException("Sources list is missing or empty");

        // targets
        if (root.DeserializeOrDefault<List<string>>("targets", options: options) is { Count: > 0 } targets)
            backupJob.Targets = targets.Select(Directory.CreateDirectory).ToList();
        else
            throw new JsonException("Targets list is missing or empty");

        // ancestry check
        if (backupJob.Targets.Any(target => backupJob.Sources.Any(source => FileSystemUtils.AreDirectAncestors(source, target))))
            throw new ArgumentException("Targets cannot be direct ancestors of sources (and vice versa).");

        // timing
        backupJob.Timing = root.DeserializeOrDefault<CronExpression>("timing", options: options)!;

        // retention
        backupJob.Retention = root.DeserializeOrDefault<BackupRetention>("retention", new(), options)!;

        // method
        backupJob.Method = root.DeserializeOrDefault<BackupMethod>("method", options: options);

        if (backupJob.Method == BackupMethod.Full)
            backupJob.Retention.Size = 1;

        return backupJob;
    }

    public override void Write(Utf8JsonWriter writer, BackupJob value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}
