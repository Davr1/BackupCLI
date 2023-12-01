using System.Security.Cryptography;

namespace BackupCLI;

public static class FileSystemUtils
{
    private static readonly MD5 Hash = MD5.Create();
    public static readonly EnumerationOptions TopLevelOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
    public static readonly EnumerationOptions RecursiveOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true };

    public static void CopyTo(this DirectoryInfo source, string target)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);
        
        foreach (var file in source.EnumerateFiles("*", TopLevelOptions))
        {
            file.CopyTo(Path.Join(target, file.Name), true);
            file.Attributes &= ~FileAttributes.Archive;
        }

        foreach (var dir in source.EnumerateDirectories("*", TopLevelOptions))
            dir.CopyTo(Path.Join(target, dir.Name));
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
}