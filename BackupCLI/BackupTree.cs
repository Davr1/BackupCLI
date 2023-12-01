namespace BackupCLI;

public class BackupTree
{
    public List<DirectoryInfo> Sources { get; }
    public Dictionary<string, int> Tree { get; } = new();

    private string GetPath(int index, string relativePath) => Path.Join(Sources[index].FullName, relativePath);

    public string? GetFilePath(string relativePath)
    {
        if (Sources.Count == 1) return Path.Join(Sources[0].FullName, relativePath);

        return Tree.TryGetValue(relativePath.ToLower(), out int idx) ? GetPath(idx, relativePath) : null;
    }

    public string? GetDirPath(string relativePath)
    {
        if (Sources.Count == 1) return Path.Join(Sources[0].FullName, relativePath);

        return Tree.TryGetValue(relativePath.ToLower() + "\\", out int idx) ? GetPath(idx, relativePath) : null;
    }

    public BackupTree(params DirectoryInfo[] sources) : this(sources.ToList()) { }

    public BackupTree(List<DirectoryInfo> sources)
    {
        Sources = sources;

        if (Sources.Count <= 1) return;

        foreach (var (dir, index) in Sources.Select((dir, i) => (dir, i)))
        foreach (var fsInfo in dir.EnumerateFileSystemInfos("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = fsInfo.FullName.Replace(dir.FullName, "").ToLower();

            if (fsInfo.Attributes.HasFlag(FileAttributes.Directory)) relativePath += "\\";

            Tree[relativePath] = index;
        }
    }
}