using BackupCLI.Collections;
using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

public class TargetDirectory(DirectoryInfo folder, BackupRetention retention, BackupMethod method, List<string> paths)
    : MetaDirectory<TargetDirectoryJson>(
        folder,
        "metadata.json",
        new TargetDirectoryJson { Packages = folder.GetDirectories().Select(dir => dir.Name).ToList() }
    )
{
    public FixedQueue<Package> Packages { get; } = new(retention.Count);
    public BackupRetention Retention { get; } = retention;
    public BackupMethod Method { get; } = method;
    public List<string> SourcePaths { get; } = paths;

    protected override void OnLoad(TargetDirectoryJson json)
    {
        foreach (var path in json.Packages)
            if (Directory.Exists(Path.Join(Folder.FullName, path))) CreatePackage(path);
    }

    private void CreatePackage(string name)
    {
        var packageDir = new DirectoryInfo(Path.Join(Folder.FullName, name));
        var paths = SourcePaths.Select(s => (s, FileSystemUtils.GetHashedPath(s.ToLower(), true))).ToDictionary();

        Packages.Enqueue(new Package(packageDir, Retention, Method, new() { Paths = paths }));
        
        SaveMetadata(new TargetDirectoryJson { Packages = Packages.Select(p => p.Folder.Name).ToList() });
    }

    public Package GetLatestPackage()
    {
        if (Packages.Last is null || Packages.Last.IsFull()) CreatePackage(DateTime.Now.Ticks.ToString());

        return Packages.Last!;
    }

    public (Package package, Dictionary<string, DirectoryInfo> targets) CreateBackup()
        => (GetLatestPackage(), GetLatestPackage().CreateBackup());
}

public class TargetDirectoryJson
{
    public List<string> Packages { get; set; } = new();
    public string? CurrentPackage => Packages.LastOrDefault();
}