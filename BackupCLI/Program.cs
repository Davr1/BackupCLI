using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Backup;
using BackupCLI.Helpers;

namespace BackupCLI;

public class Program
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new JsonListConverter<BackupJobJson>() }
    };

    public static readonly CustomLogger Logger = new("latest.log");

    static void Main(string[] args)
    {
        if (!JsonManipulator.TryLoadFile("../../../example.json", Options, out JsonList<BackupJobJson>? json)) return;

        List<BackupJob> jobs = json.Items
            .Select(obj =>
            {
                BackupJob.TryCreate((BackupJobJson)obj, out BackupJob? job);
                return job;
            })
            .Where(job => job is not null)
            .ToList();
        jobs.ForEach(job => job.PerformBackup());
    }
}