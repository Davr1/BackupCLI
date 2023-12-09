﻿using BackupCLI.Backup;
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

        if (!JsonUtils.TryLoadFile(options.File, BackupJobJsonConverter.Options, out List<BackupJob>? jobs)) return;

        Logger.Info($"Sucsessfully loaded {jobs!.Count} jobs");

        var a = new TargetDirectory(new("C:\\Users\\gpjmp\\Desktop\\test"), new BackupRetention());

        for (int i = 0; i < 50; i++)
        {
            a.CreateBackup(i.ToString());
        }

        //jobs.ForEach(job => job.PerformBackup());
    }
}