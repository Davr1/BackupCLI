namespace BackupCLI.Helpers.FileSystem;

public class Options
{
    public static readonly EnumerationOptions TopLevel = new()
    {
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive,
        AttributesToSkip = FileAttributes.System
    };

    public static readonly EnumerationOptions Recursive = new()
    {
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive,
        AttributesToSkip = FileAttributes.System | FileAttributes.ReparsePoint,
        RecurseSubdirectories = true
    };
}
