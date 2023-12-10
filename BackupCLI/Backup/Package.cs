using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

public class Package(DirectoryInfo folder, BackupRetention retention, BackupMethod method, PackageJson packageJson)
    : MetaDirectory<PackageJson>(folder, "package.json", packageJson), IDisposable
{
    public BackupRetention Retention { get; } = retention;
    public BackupMethod Method { get; } = method;
    public Dictionary<string, FileTree> Contents { get; } = new();
    public int Size => Json.Parts.Count;

    public bool IsFull()
        => Size >= (Method == BackupMethod.Full ? 1 : Retention.Size);

    protected override void OnLoad(PackageJson? json)
    {
        foreach (var (path, hash) in Json.Paths)
        {
            if (Contents.ContainsKey(path)) continue;

            var parts = GetBackupParts(hash);
            Contents[path] = method == BackupMethod.Incremental ? new FileTree(parts) : new FileTree(parts.Take(1));
        }
    }

    public Dictionary<string, DirectoryInfo> CreateBackup()
    {
        var backups = new Dictionary<string, DirectoryInfo>();

        string backupName = $"{(Json.Parts.Count == 0 ? "FULL" : Method.ToString().ToUpper()[..4])}-{DateTime.Now.Ticks}";

        var backupFolder = Folder.CreateSubdirectory(backupName);

        foreach (var (path, hash) in Json.Paths)
            backups[path] = backupFolder.CreateSubdirectory(hash);

        Json.Parts.Add(backupFolder.Name);
        
        SaveMetadata();

        return backups;
    }

    public void Dispose() => Folder.Delete(true);

    public List<DirectoryInfo> GetBackupParts(string name)
        => Json.Parts
            .Select(dir => new DirectoryInfo(Path.Join(Folder.FullName, dir, name)))
            .OrderBy(dir => dir.CreationTime)
            .ToList();
}

public class PackageJson
{
    public Dictionary<string, string> Paths { get; set; } = new();
    public List<string> Parts { get; set; } = new();
    public DateTime LastWriteTime => DateTime.Now;
}