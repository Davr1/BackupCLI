using BackupCLI.Collections;
using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

/// <summary>
/// Represents a file structure of a single full copy (and optionally multiple partial copies) which can be used to reconstruct the original file structure.
/// </summary>
/// <param name="folder">The target folder where a package will be created</param>
/// <param name="retention">The maximum amount of backups a package can hold is specified by the <see cref="BackupRetention.Size"/></param>
/// <param name="method">See <see cref="BackupMethod"/></param>
/// <param name="packageJson">The default config for the local metadata file</param>
public class Package(DirectoryInfo folder, BackupRetention retention, BackupMethod method, PackageJson packageJson)
    : MetaDirectory<PackageJson>(folder, "package.json", packageJson), IDisposable
{
    public BackupRetention Retention { get; } = retention;
    public BackupMethod Method { get; } = method;
    public Dictionary<string, FileTree> Contents { get; } = new();
    public int Size => Json.Backups.Count;

    public bool IsFull() => Size >= (Method == BackupMethod.Full ? 1 : Retention.Size);

    protected override void OnLoad(PackageJson? json)
    {
        foreach (var (path, hash) in Json.Paths)
        {
            if (Contents.ContainsKey(path)) continue;

            var parts = GetBackupParts(hash);
            Contents[path] = new FileTree(Method == BackupMethod.Incremental ? parts : parts.Take(1));
        }
    }

    /// <summary>
    /// Creates the folder structure for a backup based on the <see cref="MetaDirectory{TJson}.Json"/> config
    /// </summary>
    /// <returns>A mapping of the source paths and their respective backup folders</returns>
    public Dictionary<string, DirectoryInfo> CreateBackupFolders()
    {
        var backups = new Dictionary<string, DirectoryInfo>();

        // the full backup creation time is the same as the parent folder's
        string backupName = Json.Backups.Count == 0 ? "FULL" : $"{Method.ToString().ToUpper()[..4]}-{DateTime.Now.Ticks:X}";

        var backupFolder = Folder.CreateSubdirectory(backupName);

        foreach (var (path, hash) in Json.Paths)
            backups[path] = backupFolder.CreateSubdirectory(hash);

        Json.Backups.Add(backupFolder.Name);
        
        SaveMetadata();

        return backups;
    }

    /// <summary>
    /// Deletes the package folder and all its contents. This is only ever called by <see cref="FixedQueue{T}"/> when old packages are removed from the queue.
    /// </summary>
    public void Dispose() => Folder.Delete(true);

    public List<DirectoryInfo> GetBackupParts(string name)
        => Json.Backups
            .Select(dir => new DirectoryInfo(Path.Join(Folder.FullName, dir, name)))
            .OrderBy(dir => dir.CreationTime)
            .ToList();
}

public class PackageJson(Dictionary<string, string>? paths = null, List<string>? backups = null)
{
    public Dictionary<string, string> Paths { get; set; } = paths ?? new();
    public List<string> Backups { get; set; } = backups ?? new();
    public DateTime LastWriteTime => DateTime.Now;
}
