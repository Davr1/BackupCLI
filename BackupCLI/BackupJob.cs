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
        foreach (var source in Sources.Where(s => !Directory.Exists(s)))
            throw new DirectoryNotFoundException($"Source directory {source} does not exist.");
        if (Sources.Count == 0) throw new ArgumentException("Sources list cannot be empty.");

        Targets = json.Targets;
        foreach (var target in Targets.Where(t => !Directory.Exists(t)))
            Directory.CreateDirectory(target);
        if (Targets.Count == 0) throw new ArgumentException("Targets list cannot be empty.");

        List<string> parts = json.Timing.Split(' ').ToList();

        // standard cron expression are incompatible with the quartz format, so we need to convert them
        if (parts.Count == 5) parts.Insert(0, "0");

        // day of week and day of month are mutually exclusive
        if (parts[3] != "?" && parts[5] != "?") parts[5] = "?";

        Timing = new CronExpression(string.Join(' ', parts));

        Retention = json.Retention;

        Method = json.Method;
    }

    public void PerformBackup()
    {
        var time = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        foreach (var source in Sources)
            foreach (var target in Targets)
                new DirectoryInfo(source).CopyTo(Path.Join(target, time));
    }   
}