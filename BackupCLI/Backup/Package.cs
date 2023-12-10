using BackupCLI.FileSystem;
using BackupCLI.Helpers;

namespace BackupCLI.Backup;

public class Package(DirectoryInfo folder, BackupRetention retention, BackupMethod method, Dictionary<string, string> sourcePaths)
    : MetaDirectory<Dictionary<string, string>>(folder, "package.json", sourcePaths), IDisposable
{
    public BackupRetention Retention { get; } = retention;
    public BackupMethod Method { get; } = method;
    public int Size => Subdirectories.Count;
    public Dictionary<string, FileTree> Contents { get; } = new();
    public Dictionary<string, string> SourcePaths { get; } = sourcePaths;

    public bool IsFull()
        => Size >= (Method == BackupMethod.Full ? 1 : Retention.Size);

    protected override void SetProperties(Dictionary<string, string>? json)
    {
        if (json is null) return;

        foreach (var (path, hash) in json)
            if (Directory.Exists(Path.Join(Folder.FullName, hash)))
                Contents[path] = new FileTree(GetBackupParts(hash));
    }

    public (Package package, DirectoryInfo backup, Dictionary<string, DirectoryInfo> targets) CreateBackup()
    {
        var backups = new Dictionary<string, DirectoryInfo>();

        var backupFolder = Folder.CreateSubdirectory($"{Method}-{DateTime.Now.Ticks}");

        foreach (var (path, hash) in SourcePaths)
        {
            backups[path] = backupFolder.CreateSubdirectory(hash);

            if (!Contents.ContainsKey(path)) 
                Contents[path] = new FileTree(backups[path]);
        }

        return (this, backupFolder, backups);
    }

    public void Dispose() => Folder.Delete(true);

    public List<DirectoryInfo> GetBackupParts(string name)
        => Folder.GetDirectories().Select(dir => new DirectoryInfo(Path.Join(dir.FullName, name))).OrderBy(dir => dir.CreationTime).ToList();
}