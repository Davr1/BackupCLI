using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Backup;
using BackupCLI.Helpers.FileSystem;

namespace BackupCLI.Helpers.Json.Converters;

/// <summary>
/// Provides a custom parser for the entire json file, and checks for duplicate targets.
/// </summary>
public class BackupJobListConverter : JsonConverter<List<BackupJob>>
{
    public override List<BackupJob>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using JsonDocument document = JsonDocument.ParseValue(ref reader);

        var root = document.RootElement;

        var jobs = new List<BackupJob?>();

        // while not recommended, it is possible to create a json file with a single backupjob in the root rather than being contained within an array
        if (root.ValueKind == JsonValueKind.Object && root.Deserialize<BackupJob>(options) is { } backupJob)
            jobs.Add(backupJob);
        else if (root.ValueKind == JsonValueKind.Array && root.Deserialize<BackupJob?[]>(options) is { } backupJobs)
            jobs.AddRange(backupJobs);
        else return null;

        jobs.RemoveAll(job => job is null);

        var targets = jobs.SelectMany(job => job!.Targets).ToList();
        var uniqueTargets = targets.DistinctBy(t => FileSystemUtils.NormalizePath(t.FullName.ToLower(), true)).ToList();

        if (targets.Count != uniqueTargets.Count)
            throw new JsonException("There cannot be any duplicate targets.");

        return jobs!;
    }

    public override void Write(Utf8JsonWriter writer, List<BackupJob> value, JsonSerializerOptions options)
        => throw new NotImplementedException();
}
