using BackupCLI.Helpers.Collections;
using BackupCLI.Helpers.FileSystem;

namespace BackupCLI.Backup;

/// <summary>
/// Represents the target directory where <see cref="Package"/>s are saved to.
/// </summary>
/// <param name="folder">The target folder</param>
/// <param name="retention">The maximum amount of packages this directory can hold is specified by <see cref="BackupRetention.Count"/></param>
/// <param name="method">See <see cref="BackupMethod"/></param>
/// <param name="paths">The source directories that are copied into the packages</param>
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
        
        SaveMetadata(new([..Packages.Select(p => p.Folder.Name)]));
    }

    public Package GetLatestPackage()
    {
        if (Packages.Last is null || Packages.Last.IsFull()) CreatePackage($"{DateTime.Now.Ticks:X}");

        return Packages.Last!;
    }
}

public class TargetDirectoryJson(List<string>? packages = null)
{
    public List<string> Packages { get; set; } = packages ?? [];
    public string? CurrentPackage => Packages.LastOrDefault();
}
