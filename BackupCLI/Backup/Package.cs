namespace BackupCLI.Backup;

public class Package : IDisposable
{
    private const string MetaFileName = "package.txt";
    public DirectoryInfo Folder { get; }
    private BackupRetention Retention { get; }

    public bool IsFull() => Folder.GetDirectories().Length >= Retention.Size;

    public Package(DirectoryInfo folder, BackupRetention retention)
    {
        Folder = folder;
        Retention = retention;
    }

    public void CreateBackup(string name)
    {
        var backupDir = Directory.CreateDirectory(Path.Join(Folder.FullName, name));
        File.WriteAllLines(Path.Join(backupDir.FullName, MetaFileName), new[] { "FULL" });
    }

    public void Dispose() => Folder.Delete(true);
}