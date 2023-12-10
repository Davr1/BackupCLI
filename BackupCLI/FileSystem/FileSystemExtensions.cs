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

    public static bool TryCopyTo(this FileSystemInfo source, string destName, bool overwrite = false)
    {
        try
        {
            source.CopyTo(destName, overwrite);
            return true;
        }
        catch (Exception e)
        {
            Program.Logger.Error(e);
            return false;
        }
    }

    public static DirectoryInfo CopyTo(this DirectoryInfo source, string destDirName, bool overwrite = false)
    {
        var dir = new DirectoryInfo(destDirName);

        if (!dir.Exists || overwrite)
        {
            dir.Create();
            dir.Attributes = source.Attributes;

            foreach (var entry in source.EnumerateFileSystemInfos("*", FileSystemUtils.TopLevelOptions))
                entry.TryCopyTo(Path.Join(destDirName, entry.Name), overwrite);
        }

        return dir;
    }
}
