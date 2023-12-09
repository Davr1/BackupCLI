using BackupCLI.FileSystem;
using Quartz;

namespace BackupCLI.Backup;

public class BackupJob
{
    public List<DirectoryInfo> Sources { get; set; } = null!;
    public List<DirectoryInfo> Targets { get; set; } = null!;
    public CronExpression Timing { get; set; } = null!;
    public BackupRetention Retention { get; set; } = null!;
    public BackupMethod Method { get; set; } = default;

    public void PerformBackup()
    {
        string dirName = $"{DateTime.Now:yyyy-MM-dd_HH-mm-ss}";

        var watch = System.Diagnostics.Stopwatch.StartNew();

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

            try
            {
                BackupDirectory(source, target, packageParts);
            }
            catch (Exception e)
            {
                Program.Logger.Info("Backup failed");
                Program.Logger.Error(e);
            }
        }

        // this speeds up the process by using the simplest algorithm to mirror the primary target
        foreach (var target in Targets.Skip(1))
            foreach (var sub in primaryTarget.GetDirectories("#*"))
            {
                var dest = Path.Join(target.FullName, sub.Name);
                if (!Directory.Exists(dest)) sub.TryCopyTo(dest, true);
            }

        watch.Stop();
        Program.Logger.Info($"Took {watch.ElapsedMilliseconds} ms");
    }

    //todo: move to separate class
    private static List<DirectoryInfo> GetPackageContents(DirectoryInfo dir, string searchPattern = "*") =>
        dir.GetDirectories(searchPattern, FileSystemUtils.TopLevelOptions).OrderBy(d => d.CreationTime).ToList();

    //todo: move to separate class
    private static List<DirectoryInfo> GetBackups(DirectoryInfo dir, string sourceName, string searchPattern = "#*") =>
        GetPackageContents(dir, searchPattern).Select(dir => dir.GetDirectories(sourceName).First()).ToList();

    //todo: move to separate class
    private static DirectoryInfo? GetFullBackup(DirectoryInfo dir, string sourceName) =>
        GetBackups(dir, sourceName, "#FULL*").LastOrDefault();

    private static void BackupDirectory(DirectoryInfo source, string target, FileTree packageParts)
    {
        Directory.CreateDirectory(target);

        // there is nothing to compare, copy the folder right away
        if (packageParts.Sources.Count == 0)
        {
            source.TryCopyTo(target, true);
            return;
        }

        // copy all the new folders
        foreach (var dir in source.EnumerateDirectories("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = FileSystemUtils.GetRelativePath(source, dir);

            if (!Directory.Exists(packageParts.GetFullPath(relativePath + "\\")) &&
                !Directory.Exists(Path.Join(target, relativePath)))
                dir.TryCopyTo(Path.Join(target, relativePath), true);
        }

        foreach (var file in source.EnumerateFiles("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = FileSystemUtils.GetRelativePath(source, file);

            // file was copied in the previous step
            if (File.Exists(Path.Join(target, relativePath))) continue;

            // file was present in the previous backups
            if (packageParts.GetFile(relativePath) is FileInfo backupFile)
            {
                // compare metadata and hash
                if (FileSystemUtils.AreIdentical(file, backupFile)) continue;
            }

            // copy the file
            Directory.CreateDirectory(Path.Join(target, relativePath[..^file.Name.Length]));
            file.TryCopyTo(Path.Join(target, relativePath));
        }
    }
}
public class BackupRetention
{
    public int Count { get; set; } = 5;
    public int Size { get; set; } = 5;
}

public enum BackupMethod
{
    Full,
    Differential,
    Incremental
}