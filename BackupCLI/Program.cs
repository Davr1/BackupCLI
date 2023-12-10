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

    public static CustomLogger Logger { get; set; } = null!;

    private static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Execute);

    public static void Execute(Options options)
    {
        Logger = new CustomLogger(options.DebugLog, options.Quiet);
        
        Logger.Info("Loading jobs...");

        if (!JsonUtils.TryLoadFile(options.File, out List<BackupJob>? jobs, BackupJobJsonConverter.Options)) return;

        Logger.Info($"Successfully loaded {jobs!.Count} jobs");

        jobs.ForEach(job => job.PerformBackup());
    }
}