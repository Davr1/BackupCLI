using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

public class Package : IDisposable
{
    private const string MetaFileName = "package.txt";
    public DirectoryInfo Folder { get; }
    public BackupRetention Retention { get; }
    public BackupMethod Method { get; }
    public int Size => Folder.GetDirectories().Length;
    public FileTree Tree { get; }

    public bool IsFull()
        => Size >= (Method == BackupMethod.Full ? 1 : Retention.Size);

    public Package(DirectoryInfo folder, BackupRetention retention, BackupMethod method)
    {
        folder.Create();

        Folder = folder;
        Retention = retention;
        Method = method;
        Tree = new FileTree(folder.GetDirectories());
    }

    public (Package package, DirectoryInfo backup, Dictionary<string, DirectoryInfo> targets) CreateBackup(params string[] paths)
    {
        var backups = new Dictionary<string, DirectoryInfo>();

        var backupFolder = Folder.CreateSubdirectory($"{Method}-{DateTime.Now.Ticks}");

        foreach (var path in paths)
        {
            string name = $"{new DirectoryInfo(path).Name} {FileSystemUtils.GetHashedPath(path, true)}";

            backups[path] = backupFolder.CreateSubdirectory(name);
        }

        UpdateIndex(backupFolder);

        return (this, backupFolder, backups);
    }

    public void UpdateIndex(DirectoryInfo backup) => Tree.Add(backup);

    public void Dispose() => Folder.Delete(true);
}