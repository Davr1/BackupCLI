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
        {
            FileTree packageParts;

            switch (Method)
            {
                case BackupMethod.Incremental when GetBackups(primaryTarget, source.Name) is { Count: > 0 } backups:
                    dirName = $"#INCR_{dirName}";
                    packageParts = new FileTree(backups);
                    break;

                case BackupMethod.Differential when GetFullBackup(primaryTarget, source.Name) is { } fullBackup:
                    dirName = $"#DIFF_{dirName}";
                    packageParts = new FileTree(fullBackup);
                    break;

                case BackupMethod.Full:
                default:
                    dirName = $"#FULL_{dirName}";
                    packageParts = new FileTree();
                    break;
            }

            string target = Path.Join(primaryTarget.FullName, dirName, source.Name);

            Backup(source, target, packageParts);
        }

        // this speeds up the process by using the simplest algorithm to mirror the primary target
        foreach (var target in Targets.Skip(1))
        foreach (var sub in primaryTarget.GetDirectories("#*"))
        {
            var dest = Path.Join(target.FullName, sub.Name);
            if (!Directory.Exists(dest)) sub.CopyTo(dest);
        }
    }

    private static List<DirectoryInfo> GetPackageContents(DirectoryInfo dir, string searchPattern = "*") =>
        dir.GetDirectories(searchPattern, FileSystemUtils.TopLevelOptions).OrderBy(d => d.CreationTime).ToList();

    private static DirectoryInfo? GetFullBackup(DirectoryInfo dir, string sourceName) =>
        GetPackageContents(dir, "#FULL")
            .LastOrDefault(dir => dir.GetDirectories(sourceName).Any());

    private static List<DirectoryInfo> GetBackups(DirectoryInfo dir, string sourceName) =>
        GetPackageContents(dir, "#*")
            .Select(dir => dir.GetDirectories(sourceName).First())
            .ToList();
    
    public static void Backup(DirectoryInfo source, string target, FileTree packageParts)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        // there is nothing to compare, copy the folder right away
        if (packageParts.Sources.Count == 0)
        {
            source.CopyTo(target);
            return;
        }

        // copy all the new folders
        foreach (var dir in source.EnumerateDirectories("*", FileSystemUtils.RecursiveOptions))
        {
            var relativePath = dir.FullName.Replace(source.FullName, "");

            if (packageParts.GetFullPath(relativePath + "\\") is null && !Directory.Exists(Path.Join(target, relativePath)))
                dir.CopyTo(Path.Join(target, relativePath));
        }

        foreach (var file in source.EnumerateFiles("*", FileSystemUtils.RecursiveOptions))
        {
            var relativePath = file.FullName.Replace(source.FullName, "");

            // file was copied in the previous step
            if (File.Exists(Path.Join(target, relativePath))) continue;

            // file was present in the previous backup packages
            if (packageParts.GetFullPath(relativePath) is string path)
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