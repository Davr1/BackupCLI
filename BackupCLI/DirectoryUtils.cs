namespace BackupCLI;

public static class DirectoryUtils
{
    public static void CopyTo(this DirectoryInfo source, string target)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var file in source.EnumerateFiles())
            file.CopyTo(Path.Combine(target, file.Name), true);

        foreach (var dir in source.EnumerateDirectories())
            dir.CopyTo(Path.Combine(target, dir.Name));
    }

    public static void CopyDiff(this DirectoryInfo source, string target){
    }

    public static void CopyIncr(this DirectoryInfo source, string target){
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
}