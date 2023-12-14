using BackupCLI.Helpers.FileSystem;

namespace BackupCLI.Helpers.Extensions;

/// <summary>
/// Extension methods for <see cref="FileSystemInfo"/>. Allows for recursively copying directories, and handling errors while doing so.
/// </summary>
public static class FileSystemExtensions
{
    /* this is a bit scuffed, but FileInfo and DirectoryInfo can't be extended, so I really don't know a cleaner way to handle the different types
    A wrapper class/struct could be used, but more memory allocations and boxing/unboxing would be required, so I'm not sure if it's worth it*/
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

    /// <summary>
    /// Recursively copies a directory.
    /// </summary>
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
