namespace BackupCLI.FileSystem;

/// <summary>
/// Extension methods for <see cref="FileSystemInfo"/>.
/// </summary>
public static class FileSystemExtensions
{
    public static FileSystemInfo? CopyTo(this FileSystemInfo source, string destName, bool overwrite = false) =>
        source switch
        {
            FileInfo file => file.CopyTo(destName, overwrite),
            DirectoryInfo dir => dir.CopyTo(destName, overwrite),
            _ => null
        };

    public static DirectoryInfo CopyTo(this DirectoryInfo source, string destDirName, bool overwrite = false)
    {
        var dir = Directory.CreateDirectory(destDirName);

        foreach (var entry in source.EnumerateFileSystemInfos("*", FileSystemUtils.TopLevelOptions))
            entry.CopyTo(Path.Join(destDirName, entry.Name), overwrite);

        return dir;
    }
}
