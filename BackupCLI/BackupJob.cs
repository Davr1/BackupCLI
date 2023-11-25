using Quartz;

namespace BackupCLI;

public class BackupJob
{
    public List<DirectoryInfo> Sources { get; set; }

    public List<DirectoryInfo> Targets { get; set; }

    public CronExpression Timing { get; set; }

    public BackupRetention Retention { get; set; }

    public BackupMethod Method { get; set; }

    public BackupJob(BackupJobJson json)
    {
        foreach (var source in json.Sources.Where(s => !Directory.Exists(s)))
            throw new DirectoryNotFoundException($"Source directory {source} does not exist.");

        Sources = json.Sources.Select(FileSystemUtils.FromString).ToList();
        if (Sources.Count == 0) throw new ArgumentException("Sources list cannot be empty.");

        foreach (var target in json.Targets.Where(t => !Directory.Exists(t)))
            Directory.CreateDirectory(target);

        Targets = json.Targets.Select(FileSystemUtils.FromString).ToList();
        if (Targets.Count == 0) throw new ArgumentException("Targets list cannot be empty.");

        if (Targets.Any(t => Sources.Any(s => FileSystemUtils.AreDirectAncestors(s, t))))
            throw new ArgumentException("Targets cannot be direct ancestors of sources (and vice versa).");

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
        var dirName = $"{Method.ToString().ToUpper()}_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

        foreach (var source in Sources)
        foreach (var target in Targets)
            switch (Method)
            {
                case BackupMethod.Differential when target.GetDirectories().Length > 0:
                    Console.WriteLine("differential");
                    source.CopyDiff(Path.Join(target.FullName, dirName), "C:\\test\\FULL");
                    break;

                case BackupMethod.Incremental when target.GetDirectories().Length > 0:
                    Console.WriteLine("incremental");
                    break;

                case BackupMethod.Full:
                default:
                    Console.WriteLine("full");
                    source.CopyTo(Path.Join(target.FullName, dirName));
                    break;
            }
    }   
}