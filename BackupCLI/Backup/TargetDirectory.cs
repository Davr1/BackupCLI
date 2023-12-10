using BackupCLI.Collections;

namespace BackupCLI.Backup;

public class TargetDirectory
{
    private const string MetaFileName = "backup.txt";
    public FileInfo MetaFile { get; }
    public DirectoryInfo Folder { get; }
    public FixedQueue<Package> Packages { get; }
    public BackupRetention Retention { get; }
    public BackupMethod Method { get; }
    public List<string> Paths { get; }

    public TargetDirectory(DirectoryInfo folder, BackupRetention retention, BackupMethod method, List<string> paths)
    {
        Folder = folder;
        Packages = new(retention.Count);
        Retention = retention;
        Method = method;
        Paths = paths;

        MetaFile = new FileInfo(Path.Join(Folder.FullName, MetaFileName));

        if (MetaFile.Exists)
            foreach(var pkg in File.ReadAllLines(MetaFile.FullName))
                if (Directory.Exists(Path.Join(Folder.FullName, pkg)))
                    CreatePackage(pkg, false);

        SaveMeta();
    }

    private void SaveMeta()
        => File.WriteAllLines(MetaFile.FullName, Packages.Select(dir => dir.Folder.Name));

    private void CreatePackage(string name, bool update = true)
    {
        var packageDir = new DirectoryInfo(Path.Join(Folder.FullName, name));
        Packages.Enqueue(new Package(packageDir, Retention, Method, Paths));
        
        if (update) SaveMeta();
    }

    public Package GetLatestPackage()
    {
        if (Packages.Last is null || Packages.Last.IsFull()) CreatePackage(DateTime.Now.Ticks.ToString());

        return Packages.Last!;
    }

    public (Package package, DirectoryInfo backup, Dictionary<string, DirectoryInfo> targets) CreateBackup()
        => GetLatestPackage().CreateBackup();
}