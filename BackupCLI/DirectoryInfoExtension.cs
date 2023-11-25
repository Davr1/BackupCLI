namespace BackupCLI;

public static class DirectoryInfoExtension
{
    public static void CopyTo(this DirectoryInfo source, string target) => source.CopyTo(new DirectoryInfo(target));

    public static void CopyTo(this DirectoryInfo source, DirectoryInfo target)
    {
        if (!target.Exists) target.Create();

        foreach (var file in source.EnumerateFiles())
            file.CopyTo(Path.Combine(target.FullName, file.Name), true);

        foreach (var dir in source.EnumerateDirectories())
            dir.CopyTo(Path.Combine(target.FullName, dir.Name));
    }
}