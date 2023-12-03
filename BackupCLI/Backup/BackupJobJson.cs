using BackupCLI.Helpers;

namespace BackupCLI.Backup;

public class BackupJobJson : ValidJson
{
    public List<string> Sources { get; set; } = [];
    public List<string> Targets { get; set; } = [];
    public string Timing { get; set; } = null!;
    public BackupRetention Retention { get; set; } = new();
    public BackupMethod Method { get; set; } = BackupMethod.Full;
}

public class BackupRetention : ValidJson
{
    public int Count { get; set; } = 2;
    public int Size { get; set; } = 1;
}

public enum BackupMethod
{
    Full,
    Differential,
    Incremental
}