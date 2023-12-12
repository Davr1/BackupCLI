using BackupCLI.Collections;
using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

public class TargetDirectory(DirectoryInfo folder, BackupRetention retention, BackupMethod method, List<string> paths)
    : MetaDirectory<TargetDirectoryJson>(folder, "metadata.json", new(FileSystemUtils.GetOrderedSubdirectories(folder)))
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
        var packageDir = Directory.CreateDirectory(Path.Join(Folder.FullName, name));

        var paths = FileSystemUtils.GetHashedPaths(SourcePaths);
        var defaultJson = new PackageJson(paths, FileSystemUtils.GetOrderedSubdirectories(packageDir));

        var pkg = new Package(packageDir, Retention, Method, defaultJson);

        Packages.Enqueue(pkg);
        
        SaveMetadata(new(Packages.Select(p => p.Folder.Name).ToList()));
    }

    public Package GetLatestPackage()
    {
        if (Packages.Last is null || Packages.Last.IsFull()) CreatePackage($"{DateTime.Now.Ticks:X}");

        return Packages.Last!;
    }

    public (Package package, Dictionary<string, DirectoryInfo> targets) CreateBackup()
        => (GetLatestPackage(), GetLatestPackage().CreateBackup());
}

public class TargetDirectoryJson(List<string>? packages = null)
{
    public List<string> Packages { get; set; } = packages ?? new();
    public string? CurrentPackage => Packages.LastOrDefault();
}
