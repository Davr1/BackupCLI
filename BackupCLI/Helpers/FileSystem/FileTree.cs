namespace BackupCLI.Helpers.FileSystem;

/// <summary>
/// Simulated flattened file system tree with references to the actual files from multiple sources.
/// </summary>
public class FileTree
{
    private readonly List<DirectoryInfo> _sources = [];

    /// <summary>
    /// Relative paths mapped to the index of the source directory they belong to. Directory paths have a trailing backslash.
    /// <example>
    /// <code>Tree["/some/file/path"]</code>
    /// => <c>2</c>
    /// </example>
    /// </summary>
    private readonly Dictionary<string, int> _tree = new();

    public int Count => _sources.Count;

    public string GetFullPath(int index, string relativePath) => Path.Join(_sources[index].FullName, relativePath);

    public string? GetFullPath(string relativePath)
    {
        if (_sources.Count == 1) return GetFullPath(0, relativePath);

        return _tree.TryGetValue(relativePath.ToLower(), out int idx) ? GetFullPath(idx, relativePath) : null;
    }

    public FileInfo? GetFile(string relativePath)
        => GetFullPath(relativePath) is string path ? new(path) : null;

    public DirectoryInfo? GetDirectory(string relativePath)
        => GetFullPath(relativePath + Path.DirectorySeparatorChar) is string path ? new(path) : null;

    public FileTree(params DirectoryInfo[] sources) => AddRange(sources);

    public void AddRange(params DirectoryInfo[] sources)
    {
        foreach (var source in sources) Add(source);
    }

    public void Add(DirectoryInfo source)
    {
        _sources.Add(source);
        source.Create();

        var sourceDir = FileSystemUtils.NormalizePath(source.FullName, true);

        foreach (var fsInfo in source.EnumerateFileSystemInfos("*", Options.Recursive))
        {
            string relativePath = fsInfo.FullName.Replace(sourceDir, "").ToLower();

            if (fsInfo.Attributes.HasFlag(FileAttributes.Directory))
                relativePath += Path.DirectorySeparatorChar;

            _tree[relativePath] = _sources.Count - 1;
        }
    }
}
