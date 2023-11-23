namespace BackupCLI;

public class Program
{
    static void Main(string[] args)
    {
        var backupJobs = JSONManipulator.LoadFile("../../../example.json");
    }
}