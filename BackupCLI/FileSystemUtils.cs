using System.Security.Cryptography;

namespace BackupCLI;

public static class FileSystemUtils
{
    private static readonly MD5 Hash = MD5.Create();
    public static readonly EnumerationOptions TopLevelOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
    public static readonly EnumerationOptions RecursiveOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true };

    public static void CopyTo(this DirectoryInfo source, string target, bool recursive = true)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);
        
        foreach (var file in source.EnumerateFiles("*", TopLevelOptions))
        {
            file.CopyTo(Path.Join(target, file.Name), true);
            file.Attributes &= ~FileAttributes.Archive;
        }

        if (!recursive) return;

        foreach (var dir in source.EnumerateDirectories("*", TopLevelOptions))
            dir.CopyTo(Path.Join(target, dir.Name));
    }

    public static void CopyIncr(this DirectoryInfo source, string target, BackupTree backups)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var dir in source.EnumerateDirectories("*", RecursiveOptions))
        {
            var relativePath = dir.FullName.Replace(source.FullName, "");

            if (backups.GetDirPath(relativePath) is null && !Directory.Exists(Path.Join(target, relativePath)))
                dir.CopyTo(Path.Join(target, relativePath));
        }

        foreach (var file in source.EnumerateFiles("*", RecursiveOptions))
        {
            var relativePath = file.FullName.Replace(source.FullName, "");

            // file was copied in the previous step
            if (File.Exists(Path.Join(target, relativePath))) continue;

            // file was present in the last full backup
            if (backups.GetFilePath(relativePath) is string path)
            {
                var lastFullBackupFile = new FileInfo(path);

                // the modified time and the size is the same, so the file was not changed
                if (file.LastWriteTime == lastFullBackupFile.LastWriteTime &&
                    file.Length == lastFullBackupFile.Length) continue;

                // the archival flag is disabled, so the file was not changed
                if (!file.Attributes.HasFlag(FileAttributes.Archive)) continue;

                // compare hashes in case the file was incorrectly marked as changed
                if (file.GetHash() == lastFullBackupFile.GetHash())
                {
                    file.Attributes &= ~FileAttributes.Archive;
                    continue;
                }
            }

            // remove the archival flag from the original file
            file.Attributes &= ~FileAttributes.Archive;

            // finally, copy the file
            Directory.CreateDirectory(Path.Join(target, relativePath[..^file.Name.Length]));
            file.CopyTo(Path.Join(target, relativePath));
        }
    }

    public static bool AreDirectAncestors(DirectoryInfo left, DirectoryInfo right)
    {
        if (left.FullName == right.FullName) return true;

        DirectoryInfo _left = left;
        while (_left.Parent is not null)
        {
            if (_left.Parent.FullName == right.FullName) return true;
            _left = _left.Parent;
        }
        
        DirectoryInfo _right = right;
        while (_right.Parent is not null)
        {
            if (_right.Parent.FullName == left.FullName) return true;
            _right = _right.Parent;
        }

        return false;
    }

    public static DirectoryInfo FromString(string path) => new DirectoryInfo(Path.Join(path, ".").ToLower());

    public static string GetHash(this FileInfo file)
    {
        using var stream = file.OpenRead();
        return BitConverter.ToString(Hash.ComputeHash(stream)).Replace("-", "").ToLower();
    }
    
    public static DirectoryInfo? GetLastFullBackup(DirectoryInfo dir)
    {
        var fullBackups = dir.GetDirectories("#FULL_*", TopLevelOptions).ToList();
        return fullBackups.Count == 0 ? null : fullBackups.OrderByDescending(d => d.CreationTime).First();
    }

    public static List<DirectoryInfo>? GetBackups(DirectoryInfo dir)
    {
        var fullBackups = dir.GetDirectories("#*", TopLevelOptions).ToList();
        return fullBackups.Count == 0 ? null : fullBackups.OrderBy(d => d.CreationTime).ToList();
    }
}