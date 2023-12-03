namespace BackupCLI.FileSystem;

public static class FileSystemUtils
{
    public static readonly EnumerationOptions TopLevelOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
    public static readonly EnumerationOptions RecursiveOptions = new() { IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive, RecurseSubdirectories = true };

    /// <summary>
    /// Checks whether two directories are direct ancestors of each other.
    /// <example>
    /// <code>AreDirectAncestors(new("C:\Windows"), new("C:\Windows\System32"))</code>
    /// => <c>true</c>
    /// </example>
    /// </summary>
    public static bool AreDirectAncestors(DirectoryInfo left, DirectoryInfo right)
    {
        left = FromPath(left.FullName);
        right = FromPath(right.FullName);

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

    /// <summary>
    /// Checks whether two files are identical based on their properties and content.
    /// </summary>
    public static bool AreIdentical(FileInfo left, FileInfo right)
    {
        // early return based on file metadata
        if (!left.Exists || !right.Exists) return false;
        if (left.Length == right.Length && left.LastWriteTime == right.LastWriteTime) return true;

        // the word size on a 64bit processor
        const int size = sizeof(long);

        int iterations = (int)Math.Ceiling((double)left.Length / size);

        using var leftStream = left.OpenRead();
        using var rightStream = right.OpenRead();

        byte[] leftBuffer = new byte[size];
        byte[] rightBuffer = new byte[size];

        // reads the files chunk by chunk and compares them
        for (int i = 0; i < iterations; i++)
        {
            leftStream.Read(leftBuffer, 0, size);
            rightStream.Read(rightBuffer, 0, size);

            // early return if the two chunks are not equal - this is much faster than unconditionally hashing the entire file
            if (BitConverter.ToInt64(leftBuffer, 0) != BitConverter.ToInt64(rightBuffer, 0))
                return false;
        }

        return true;
    }

    /// <returns>
    /// <see cref="DirectoryInfo"/> with a trailing slash in the .FullName
    /// </returns>
    public static DirectoryInfo FromPath(string path)
        => new (Path.TrimEndingDirectorySeparator(path) + Path.DirectorySeparatorChar);

    /// <summary>
    /// Strips the absolute path from the <paramref name="dir"/> and returns the relative path to <paramref name="path"/>.
    /// </summary>
    /// <returns></returns>
    public static string GetRelativePath(DirectoryInfo dir, FileSystemInfo path)
        => Path.GetRelativePath(dir.FullName, path.FullName);
}