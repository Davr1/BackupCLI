using BackupCLI.Helpers.Extensions;
using BackupCLI.Helpers.FileSystem;
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
        PrimaryTarget ??= new TargetDirectory(Targets.First(), Retention, Method, [..Sources.Select(s => s.FullName)]);

        var package = PrimaryTarget.GetLatestPackage();
        var targets = package.CreateBackupFolders();

        // copies the source directories to the primary target
        foreach (var source in Sources)
        {
            // only folders inside the current package.json will be copied
            // if the configuration changes, the folder will be copied in the next package
            if (!targets.TryGetValue(source.FullName, out var target)) continue;

            BackupDirectory(source, target.FullName, package.Contents[source.FullName]);

            package.Update(source.FullName, target);
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
        if (packageContent.Count == 0)
        {
            source.TryCopyTo(target, true);
            return;
        }

        foreach (var fsinfo in source.EnumerateFileSystemInfos("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = Path.GetRelativePath(source.FullName, fsinfo.FullName);
            string currentBackup = Path.Join(target, relativePath);

            if (fsinfo is DirectoryInfo)
            {
                if (Directory.Exists(currentBackup)) continue;

                // directory was present in the previous backups
                if (packageContent.GetDirectory(relativePath) is not null) continue;
            }

            if (fsinfo is FileInfo file)
            {
                if (File.Exists(currentBackup)) continue;

                // file was present in the previous backups - continue if the current file hasn't changed
                if (packageContent.GetFile(relativePath) is FileInfo backupFile)
                    if (FileSystemUtils.AreIdentical(file, backupFile)) continue;

                Directory.CreateDirectory(Path.GetDirectoryName(currentBackup)!);
            }

            fsinfo.TryCopyTo(currentBackup, true);
        }
    }
}
