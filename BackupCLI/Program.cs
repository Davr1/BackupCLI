using Quartz;

namespace BackupCLI;

public class Program
{
    static void Main(string[] args)
    {
        try
        {
            var backupJobs = JsonManipulator.LoadFile("../../../example.json");
        }
        catch (Exception e)
        {
            Console.WriteLine($"{e.GetType().Name} occurred while parsing json file:\n\t{e.Message}");
        }
    }
}