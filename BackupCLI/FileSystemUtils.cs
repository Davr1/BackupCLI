namespace BackupCLI;

public static class FileSystemUtils
{
    public static readonly EnumerationOptions TopLevelOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
    public static readonly EnumerationOptions RecursiveOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true };

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

    public static bool AreIdentical(FileInfo left, FileInfo right) =>
        left.Exists && right.Exists && 
        ((left.Length == right.Length && left.LastWriteTime == right.LastWriteTime) || left.GetHash() == right.GetHash());

    public static DirectoryInfo NormalizePath(string path) => new DirectoryInfo(Path.Join(path, ".").ToLower());

    public static string GetRelativePath(DirectoryInfo dir, FileSystemInfo path)
        => path.FullName.Replace(dir.FullName, string.Empty);
}