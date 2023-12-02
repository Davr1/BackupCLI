using System.Security.Cryptography;

namespace BackupCLI.FileSystem;

public static class FileSystemExtensions
{
    private static readonly MD5 Hash = MD5.Create();

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

    public static string GetHash(this FileInfo file)
    {
        using var stream = file.OpenRead();
        return BitConverter.ToString(Hash.ComputeHash(stream)).Replace("-", "").ToLower();
    }
}
