using System.Security.Cryptography;

namespace BackupCLI;

public static class FileSystemUtils
{
    private static readonly MD5 Hash = MD5.Create();
    private static readonly EnumerationOptions TopLevelOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
    private static readonly EnumerationOptions RecursiveOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true };

    public static void CopyTo(this DirectoryInfo source, string target, bool recursive = true)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var file in source.EnumerateFiles("*", TopLevelOptions))
        {
            file.CopyTo(Path.Join(target, file.Name), true);
            file.SetAttributes(file.GetAttributes() & ~FileAttributes.Archive);
        }

        if (!recursive) return;

        foreach (var dir in source.EnumerateDirectories("*", TopLevelOptions))
            dir.CopyTo(Path.Join(target, dir.Name));
    }

    public static void CopyIncr(this DirectoryInfo source, string target)
    {
    }

    public static void CopyDiff(this DirectoryInfo source, string target, string lastBackup)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var dir in source.EnumerateDirectories("*", RecursiveOptions))
        {
            var relativePath = dir.FullName.Replace(source.FullName, "");

            if (!Directory.Exists(Path.Join(lastBackup, relativePath)))
                dir.CopyTo(Path.Join(target, relativePath));
        }

        foreach (var file in source.EnumerateFiles("*", RecursiveOptions))
        {
            var relativePath = file.FullName.Replace(source.FullName, "");

            // file was copied in the previous step
            if (File.Exists(Path.Join(target, relativePath))) continue;

            // file was present in the last full backup
            if (new FileInfo(Path.Join(lastBackup, relativePath)) is { Exists: true } lastFullBackupFile)
            {
                // the archival flag is disabled and the modified time is the same, so the file was not changed
                if (!file.HasAttributes(FileAttributes.Archive) &&
                    file.LastWriteTime == lastFullBackupFile.LastWriteTime) continue;

                // compare hashes in case the file was incorrectly marked as changed
                if (file.GetHash() == lastFullBackupFile.GetHash())
                {
                    file.SetAttributes(file.GetAttributes() & ~FileAttributes.Archive);
                    continue;
                }
            }

            // remove the archival flag from the original file
            file.SetAttributes(file.GetAttributes() & ~FileAttributes.Archive);

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
    
    public static FileAttributes GetAttributes(this FileInfo file) => File.GetAttributes(file.FullName);
    public static bool HasAttributes(this FileInfo file, FileAttributes attributes) => (file.GetAttributes() & attributes) != 0;
    public static void SetAttributes(this FileInfo file, FileAttributes attributes) => File.SetAttributes(file.FullName, attributes);

    public static DirectoryInfo? GetLastFullBackup(DirectoryInfo dir)
    {
        var fullBackups = dir.GetDirectories("#FULL_*", TopLevelOptions).ToList();
        return fullBackups.Count == 0 ? null : fullBackups.OrderByDescending(d => d.CreationTime).First();
    }
}