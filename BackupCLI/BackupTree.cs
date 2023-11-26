namespace BackupCLI;

public class BackupTree
{
    public List<DirectoryInfo> Sources { get; }
    public Dictionary<string, int> Tree { get; } = new();

    private string GetPath(int index, string relativePath) => Path.Join(Sources[index].FullName, relativePath);

    public string? GetFilePath(string relativePath) =>
        Tree.TryGetValue(relativePath.ToLower(), out int idx) ? GetPath(idx, relativePath) : null;
    
    public string? GetDirPath(string relativePath) =>
        Tree.TryGetValue(relativePath.ToLower()+"\\", out int idx) ? GetPath(idx, relativePath) : null;

    public BackupTree(List<DirectoryInfo> sources)
    {
        Sources = sources;

        foreach (var (dir, index) in Sources.Select((dir, i) => (dir, i)))
        foreach (var fsInfo in dir.EnumerateFileSystemInfos("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = fsInfo.FullName.Replace(dir.FullName, "").ToLower();

            if (fsInfo.Attributes.HasFlag(FileAttributes.Directory)) relativePath += "\\";

            Tree[relativePath] = index;
        }
    }
}