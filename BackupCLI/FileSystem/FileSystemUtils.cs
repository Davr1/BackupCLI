namespace BackupCLI.FileSystem;

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

    public static bool AreIdentical(FileInfo left, FileInfo right)
    {
        if (!left.Exists || !right.Exists) return false;
        if (left.Length == right.Length && left.LastWriteTime == right.LastWriteTime) return true;

        int size = Environment.Is64BitProcess ? sizeof(long) : sizeof(int);
        int iterations = (int)Math.Ceiling((double)left.Length / size);

        using var leftStream = left.OpenRead();
        using var rightStream = right.OpenRead();

        byte[] leftBuffer = new byte[size];
        byte[] rightBuffer = new byte[size];

        for (int i = 0; i < iterations; i++)
        {
            leftStream.Read(leftBuffer, 0, size);
            rightStream.Read(rightBuffer, 0, size);

            if (BitConverter.ToInt64(leftBuffer, 0) != BitConverter.ToInt64(rightBuffer, 0))
                return false;
        }

        return true;
    }

    public static DirectoryInfo FromPath(string path)
        => new (Path.TrimEndingDirectorySeparator(path) + "\\");

    public static string GetRelativePath(DirectoryInfo dir, FileSystemInfo path)
        => Path.GetRelativePath(dir.FullName, path.FullName);
}