namespace BackupCLI.Backup;

public class BackupRetention
{
    /// <summary>
    /// The maximum amount of packages that a target directory can hold.
    /// </summary>
    public int Count { get; set; } = 5;
    /// <summary>
    /// The maximum amount of backups that a package can hold.
    /// </summary>
    public int Size { get; set; } = 5;
}

public enum BackupMethod
{
    /// <summary>
    /// A single package that contains a full copy of the source directories.
    /// </summary>
    Full,
    /// <summary>
    /// Multiple packages that contain the changes made after the last full package.
    /// </summary>
    Differential,
    /// <summary>
    /// Multiple packages that contain the changes made after the last full or incremental package.
    /// </summary>
    Incremental
}
