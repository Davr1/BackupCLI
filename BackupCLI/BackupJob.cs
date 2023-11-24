using Quartz;

namespace BackupCLI;

public class BackupJob
{
    public List<string> Sources { get; set; }

    public List<string> Targets { get; set; }

    public CronExpression Timing { get; set; }

    public BackupRetention Retention { get; set; }

    public BackupMethod Method { get; set; }

    public BackupJob(BackupJobJson json)
    {
        Sources = json.Sources;
        foreach (var s in Sources.Where(source => !Directory.Exists(source)))
            throw new DirectoryNotFoundException($"Source directory {s} does not exist.");

        Targets = json.Targets;
        foreach (var t in Targets.Where(source => !Directory.Exists(source)))
            Directory.CreateDirectory(t);

        List<string> parts = json.Timing.Split(' ').ToList();

        // standard cron expression are incompatible with the quartz format, so we need to convert them
        if (parts.Count == 5) parts.Insert(0, "0");

        // day of week and day of month are mutually exclusive
        if (parts[3] != "?" && parts[5] != "?") parts[5] = "?";

        Timing = new CronExpression(string.Join(' ', parts));

        Retention = json.Retention;

        Method = json.Method;
    }
}