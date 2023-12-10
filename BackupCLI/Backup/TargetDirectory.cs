using BackupCLI.Collections;
using BackupCLI.FileSystem;
using BackupCLI.Helpers;

namespace BackupCLI.Backup;

public class TargetDirectory(DirectoryInfo folder, BackupRetention retention, BackupMethod method, List<string> paths)
    : MetaDirectory<List<string>>(folder, "metadata.json", folder.GetDirectories().Select(dir => dir.Name).ToList())
{
    public FixedQueue<Package> Packages { get; } = new(retention.Count);
    public BackupRetention Retention { get; } = retention;
    public BackupMethod Method { get; } = method;
    public List<string> SourcePaths { get; } = paths;

    protected override void SetProperties(List<string>? json)
    {
        if (json is null) return;

        foreach (var path in json.Where(path => Directory.Exists(Path.Join(Folder.FullName, path))))
            CreatePackage(path, false);
    }

    private void CreatePackage(string name, bool update = true)
    {
        var packageDir = new DirectoryInfo(Path.Join(Folder.FullName, name));
        var paths = SourcePaths.Select(s => (s, FileSystemUtils.GetHashedPath(s.ToLower(), true))).ToDictionary();

        Packages.Enqueue(new Package(packageDir, Retention, Method, new() { Paths = paths }));
        
        if (update) SaveMetadata(Packages.Select(p => p.Folder.Name).ToList());
    }

    public Package GetLatestPackage()
    {
        if (Packages.Last is null || Packages.Last.IsFull()) CreatePackage(DateTime.Now.Ticks.ToString());

        return Packages.Last!;
    }

    public (Package package, Dictionary<string, DirectoryInfo> targets) CreateBackup()
        => GetLatestPackage().CreateBackup();
}