using BackupCLI.FileSystem;

namespace BackupCLI.Backup;

public class Package : IDisposable
{
    private const string MetaFileName = "package.txt";
    public FileInfo MetaFile { get; }
    public DirectoryInfo Folder { get; }
    public BackupRetention Retention { get; }
    public BackupMethod Method { get; }
    public int Size => Folder.GetDirectories().Length;
    public Dictionary<string, FileTree> Contents { get; } = new();
    public List<string> Paths { get; }

    public bool IsFull()
        => Size >= (Method == BackupMethod.Full ? 1 : Retention.Size);

    public Package(DirectoryInfo folder, BackupRetention retention, BackupMethod method, List<string> paths)
    {
        folder.Create();

        Folder = folder;
        Retention = retention;
        Method = method;
        Paths = paths;

        MetaFile = new FileInfo(Path.Join(Folder.FullName, MetaFileName));

        if (MetaFile.Exists) 
            foreach (var pkg in File.ReadAllLines(MetaFile.FullName))
            {
                Console.WriteLine(pkg);
                var parts = pkg.Split('|');
                if (parts.Length != 2) continue;
                var name = parts[0];
                var hash = parts[1];

                var parts2 = GetBackupParts(hash);
                Contents[name] = new FileTree(parts2);
            }

        SaveMeta();
    }

    public (Package package, DirectoryInfo backup, Dictionary<string, DirectoryInfo> targets) CreateBackup()
    {
        var backups = new Dictionary<string, DirectoryInfo>();

        var backupFolder = Folder.CreateSubdirectory($"{Method}-{DateTime.Now.Ticks}");

        foreach (var path in Paths)
        {
            string name = /*$"{new DirectoryInfo(path).Name} {*/GetHashedPath(path, true)/*}"*/;
            
            backups[path] = backupFolder.CreateSubdirectory(name);

            if (!Contents.ContainsKey(path)) 
                Contents[path] = new FileTree(backups[path]);
        }

        return (this, backupFolder, backups);
    }

    public void Dispose() => Folder.Delete(true);

    public void SaveMeta()
    {
        Program.Logger.Info("Saving meta");
        Program.Logger.Info(string.Join(",", Paths));
        File.WriteAllLines(MetaFile.FullName, Paths.Select(path => $"{path}|{GetHashedPath(path, true)}"));
    }
    public List<DirectoryInfo> GetBackupParts(string hash)
        => Folder.GetDirectories().Select(dir => new DirectoryInfo(Path.Join(dir.FullName, hash))).OrderBy(dir => dir.CreationTime).ToList();

    public string GetHashedPath(string path, bool isDir) => path.Replace(Path.DirectorySeparatorChar, '-')[3..] + "" + FileSystemUtils.GetHashedPath(path, isDir);
}