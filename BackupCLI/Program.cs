using System.Text.Json;
using System.Text.Json.Serialization;
using BackupCLI.Backup;
using BackupCLI.Helpers;
using CommandLine;

namespace BackupCLI;

public class Program
{
    public class Options
    {
        [Option('i', "input", Required = true, HelpText = "Path to the json file.")]
        public string File { get; set; } = null!;
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new JsonListConverter<BackupJobJson>() }
    };

    public static readonly CustomLogger Logger = new("latest.log");

    private static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Execute);

    public static void Execute(Options options)
    {
        if (!JsonManipulator.TryLoadFile(options.File, JsonOptions, out JsonList<BackupJobJson>? json)) return;

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