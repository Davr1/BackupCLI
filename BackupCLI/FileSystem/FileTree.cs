namespace BackupCLI.FileSystem;

public class FileTree
{
    public List<DirectoryInfo> Sources { get; }
    public Dictionary<string, int> Tree { get; } = new();

    public string GetFullPath(int index, string relativePath) => Path.Join(Sources[index].FullName, relativePath);

    public string? GetFullPath(string relativePath)
    {
        if (Sources.Count == 1) return GetFullPath(0, relativePath);

        return Tree.TryGetValue(relativePath.ToLower(), out int idx) ? GetFullPath(idx, relativePath) : null;
    }

    public FileInfo? GetFile(string relativePath)
        => GetFullPath(relativePath) is string path ? new(path) : null;

    public DirectoryInfo? GetDirectory(string relativePath)
        => GetFullPath(relativePath + "\\") is string path ? new(path) : null;

    public FileTree(params DirectoryInfo[] sources) : this(sources.ToList()) { }

    public FileTree(List<DirectoryInfo> sources)
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