using BackupCLI.Collections;
using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

public class TargetDirectory
{
    private const string MetaFileName = "backup.txt";
    private DirectoryInfo Folder { get; }
    public FixedQueue<Package> Packages { get; }
    public Package LatestPackage
    {
        get
        {
            if (Packages.Last is null || Packages.Last.IsFull())
                CreatePackage(DateTime.Now.Ticks.ToString());

            return Packages.Last!;
        }
    }

    private BackupRetention Retention { get; }

    public TargetDirectory(DirectoryInfo folder, BackupRetention retention)
    {
        Folder = folder;
        Packages = new(retention.Count);
        Retention = retention;

        var metaFilePath = Path.Join(Folder.FullName, MetaFileName);

        if (File.Exists(metaFilePath))
        {
            foreach (var pkg in File.ReadAllLines(metaFilePath))
            {
                if (new DirectoryInfo(Path.Join(Folder.FullName, pkg)) is { Exists: true } dir)
                {
                    Packages.Enqueue(new(dir, Retention));
                }
            }
        }
        else
        {
            File.WriteAllLines(metaFilePath, Directory.EnumerateDirectories(folder.FullName, "#*", FileSystemUtils.TopLevelOptions));
        }
    }

    private void SaveMeta()
    {
        var metaFilePath = Path.Join(Folder.FullName, MetaFileName);
        File.WriteAllLines(metaFilePath, Packages.Select(dir => dir.Folder.Name));
    }

    public void CreatePackage(string name)
    {
        var packageDir = Directory.CreateDirectory(Path.Join(Folder.FullName, name));
        Packages.Enqueue(new(packageDir, Retention));
        SaveMeta();
    }

    public void CreateBackup(string name) => LatestPackage.CreateBackup(name);
}