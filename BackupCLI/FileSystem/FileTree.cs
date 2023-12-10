using Microsoft.Extensions.Logging;

namespace BackupCLI.FileSystem;

/// <summary>
/// Simulated flattened file system tree with references to the actual files from multiple sources.
/// </summary>
public class FileTree
{
    public List<DirectoryInfo> Sources { get; } = new();

    /// <summary>
    /// Relative paths mapped to the index of the source directory they belong to. Directory paths have a trailing backslash.
    /// <example>
    /// <code>Tree["/some/file/path"]</code>
    /// => <c>2</c>
    /// </example>
    /// </summary>
    private Dictionary<string, int> Tree { get; } = new();

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

    public FileTree(params DirectoryInfo[] sources) : this(sources.AsEnumerable()) { }

    public FileTree(IEnumerable<DirectoryInfo> sources)
    {
        foreach (var source in sources) Add(source);
    }

    public void Add(DirectoryInfo source)
    {
        Sources.Add(source);
        source.Create();

        var sourceDir = FileSystemUtils.NormalizePath(source.FullName, true);

        foreach (var fsInfo in source.EnumerateFileSystemInfos("*", FileSystemUtils.RecursiveOptions))
        {
            string relativePath = fsInfo.FullName.Replace(sourceDir, "").ToLower();

            if (fsInfo.Attributes.HasFlag(FileAttributes.Directory)) relativePath += "\\";

            Tree[relativePath] = Sources.Count - 1;
        }
    }
}