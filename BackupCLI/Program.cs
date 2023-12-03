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

        [Option('d', "debug", HelpText = "Path to a log file.")]
        public string DebugLog { get; set; } = "latest.log";

        [Option('q', "quiet", HelpText = "Suppresses console logs.")]
        public bool Quiet { get; set; } = false;
    }

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(), new JsonListConverter<BackupJobJson>() }
    };

    public static CustomLogger Logger { get; set; } = null!;

    private static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Execute);

    public static void Execute(Options options)
    {
        if (!JsonManipulator.TryLoadFile(options.File, JsonOptions, out JsonList<BackupJobJson>? json)) return;

        Logger = new CustomLogger(options.DebugLog, options.Quiet);

        Logger.Info("Loading jobs...");
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