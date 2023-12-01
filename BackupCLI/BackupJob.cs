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
        string dirName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

        var primaryTarget = Targets.First();

        foreach (var source in Sources)
            switch (Method)
            {
                case BackupMethod.Incremental when GetPackageContents(primaryTarget) is { Count: > 0 } backups:
                    Backup(
                        source,
                        Path.Join(primaryTarget.FullName, $"#INCR_{dirName}", source.Name),
                        new BackupTree(backups.Select(d => new DirectoryInfo(Path.Join(d.FullName, source.Name))).ToList()));
                    break;

                case BackupMethod.Full:
                default:
                    source.CopyTo(Path.Join(Targets.First().FullName, $"#FULL_{dirName}", source.Name));
                    break;
            }

        // this speeds up the process by using the simplest algorithm to mirror the primary target
        foreach (var target in Targets.Skip(1))
        foreach (var sub in primaryTarget.GetDirectories("#*"))
        {
            var dest = Path.Join(target.FullName, sub.Name);
            if (!Directory.Exists(dest)) sub.CopyTo(dest);
        }
    }

    public static List<DirectoryInfo> GetPackageContents(DirectoryInfo dir) =>
        dir.GetDirectories("#*", FileSystemUtils.TopLevelOptions).OrderBy(d => d.CreationTime).ToList();
    
    public static void Backup(DirectoryInfo source, string target, BackupTree packageParts)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var dir in source.EnumerateDirectories("*", FileSystemUtils.RecursiveOptions))
        {
            var relativePath = dir.FullName.Replace(source.FullName, "");

            if (packageParts.GetDirPath(relativePath) is null && !Directory.Exists(Path.Join(target, relativePath)))
                dir.CopyTo(Path.Join(target, relativePath));
        }

        foreach (var file in source.EnumerateFiles("*", FileSystemUtils.RecursiveOptions))
        {
            var relativePath = file.FullName.Replace(source.FullName, "");

            // file was copied in the previous step
            if (File.Exists(Path.Join(target, relativePath))) continue;

            // file was present in the previous backup packages
            if (packageParts.GetFilePath(relativePath) is string path)
            {
                var backupFile = new FileInfo(path);

                // the modified time and the size is the same, so the file has not changed
                if (file.LastWriteTime == backupFile.LastWriteTime &&
                    file.Length == backupFile.Length) continue;

                // the archival flag is disabled, so the file has not changed
                if (!file.Attributes.HasFlag(FileAttributes.Archive)) continue;

                // compare hashes in case the file was incorrectly marked as changed
                if (file.GetHash() == backupFile.GetHash())
                {
                    file.Attributes &= ~FileAttributes.Archive;
                    continue;
                }
            }

            // remove the archival flag from the original file
            file.Attributes &= ~FileAttributes.Archive;

            // finally, copy the file
            Directory.CreateDirectory(Path.Join(target, relativePath[..^file.Name.Length]));
            file.CopyTo(Path.Join(target, relativePath));
        }
    }
}