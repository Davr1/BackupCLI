using BackupCLI.Backup;
using BackupCLI.Helpers.Json;
using CommandLine;

namespace BackupCLI;

public class Program
{
    public class Options
    {
        [Option('c', "config", Required = true, HelpText = "Path to the json config file.")]
        public string File { get; set; } = null!;

        [Option('l', "log", HelpText = "Path to a log file.")]
        public string DebugLog { get; set; } = "latest.log";

        [Option('q', "quiet", HelpText = "Suppress console logs.")]
        public bool Quiet { get; set; } = false;
    }

    public static CustomLogger Logger { get; set; } = null!;

    private static void Main(string[] args) => Parser.Default.ParseArguments<Options>(args).WithParsed(Execute);

    public static void Execute(Options options)
    {
        Logger = new CustomLogger(options.DebugLog, options.Quiet);
        
        Logger.Info("Loading jobs...");

        if (!JsonUtils.TryLoadFile(options.File, out List<BackupJob>? jobs)) return;

        Logger.Info($"Successfully loaded {jobs!.Count} jobs");
        
        Scheduler.SetupCronJobs(jobs).Wait();

        Task.Delay(-1).Wait();
    }
}
