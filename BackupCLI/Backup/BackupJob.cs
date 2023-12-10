using System.Threading.Channels;
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

        var primaryTarget = new TargetDirectory(Targets.First(), Retention, Method, Sources.Select(s => s.FullName).ToList());

        var sources = Sources.Select(source => source.FullName).ToArray();
        var (package, backup, targets) = primaryTarget.CreateBackup();

        foreach (var source in Sources)
        {
            var target = targets[source.FullName];
            BackupDirectory(source, target.FullName, package.Contents[source.FullName]);
            package.Contents[source.FullName].Add(target);
            package.SaveMeta();
        }
        
        //package.UpdateIndex(backup);

        Console.WriteLine(targets.Count);

        watch.Stop();
        Program.Logger.Info($"Took {watch.ElapsedMilliseconds} ms");
    }

    private static void BackupDirectory(DirectoryInfo source, string target, FileTree packageParts)
    {
        Console.WriteLine($"Copying {source.FullName} to {target}");
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