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
    public TargetDirectory? PrimaryTarget { get; set; }

    /// <summary>
    /// Immediately runs the backup job.
    /// </summary>
    public void PerformBackup()
    {
        PrimaryTarget ??= new TargetDirectory(Targets.First(), Retention, Method, Sources.Select(s => s.FullName).ToList());

        var package = PrimaryTarget.GetLatestPackage();
        var targets = PrimaryTarget.CreateBackupFolders();

        // copies the source directories to the primary target
        foreach (var source in Sources)
        {
            // only folders inside the current package.json will be copied
            // if the configuration changes, the folder will be copied in the next package
            if (!targets.TryGetValue(source.FullName, out var target)) continue;

            BackupDirectory(source, target.FullName, package.Contents[source.FullName]);
            package.Contents[source.FullName].Add(target);
        }

        // mirrors the primary target to other targets
        foreach (var target in Targets.Skip(1))
        {
            PrimaryTarget.MetadataFile.TryCopyTo(Path.Join(target.FullName, PrimaryTarget.MetadataFileName), true);

            foreach (var pkg in PrimaryTarget.Packages)
            {
                var mirrorPkg = Directory.CreateDirectory(Path.Join(target.FullName, pkg.Folder.Name));

                pkg.MetadataFile.TryCopyTo(Path.Join(mirrorPkg.FullName, pkg.MetadataFileName), true);

                foreach (var path in pkg.Json.Backups.Select(part => Path.Join(pkg.Folder.Name, part)))
                    new DirectoryInfo(Path.Join(PrimaryTarget.Folder.FullName, path)).CopyTo(Path.Join(target.FullName, path));
            }
        }
    }

    /// <summary>
    /// Copies the source directory to the target directory.
    /// <paramref name="packageContent"/> is used to compare the current state of the source and target directories to the previous backups in the same package.
    /// </summary>
    private static void BackupDirectory(DirectoryInfo source, string target, FileTree packageContent)
    {
        Directory.CreateDirectory(target);

        // there is nothing to compare, copy the folder right away
        if (packageContent.Sources.Count == 0)
        {
            source.TryCopyTo(target, true);
            return;
        }

        // copy all the new folders
        foreach (var dir in source.EnumerateDirectories("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = FileSystemUtils.GetRelativePath(source, dir);

            string? previousBackup = packageContent.GetFullPath(FileSystemUtils.NormalizePath(relativePath, true));
            string currentBackup = Path.Join(target, relativePath);

            if (!Directory.Exists(previousBackup) && !Directory.Exists(currentBackup))
                dir.TryCopyTo(Path.Join(target, relativePath), true);
        }

        // copy the files that were modified or added
        foreach (var file in source.EnumerateFiles("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = FileSystemUtils.GetRelativePath(source, file);

            // file was copied in the previous step
            if (File.Exists(Path.Join(target, relativePath))) continue;

            // file was present in the previous backups
            if (packageContent.GetFile(relativePath) is FileInfo backupFile)
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
    /// <summary>
    /// The maximum amount of packages that a target directory can hold.
    /// </summary>
    public int Count { get; set; } = 5;
    /// <summary>
    /// The maximum amount of backups that a package can hold.
    /// </summary>
    public int Size { get; set; } = 5;
}

public enum BackupMethod
{
    /// <summary>
    /// A single package that contains a full copy of the source directories.
    /// </summary>
    Full,
    /// <summary>
    /// Multiple packages that contain the changes made after the last full package.
    /// </summary>
    Differential,
    /// <summary>
    /// Multiple packages that contain the changes made after the last full or incremental package.
    /// </summary>
    Incremental
}
