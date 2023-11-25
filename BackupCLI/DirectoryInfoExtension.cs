namespace BackupCLI;

public static class DirectoryInfoExtension
{
    public static void CopyTo(this DirectoryInfo source, string target)
    {
        if (!Directory.Exists(target)) Directory.CreateDirectory(target);

        foreach (var file in source.EnumerateFiles())
            file.CopyTo(Path.Combine(target, file.Name), true);

        foreach (var dir in source.EnumerateDirectories())
            dir.CopyTo(Path.Combine(target, dir.Name));
    }
}