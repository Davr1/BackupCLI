namespace BackupCLI;

public class Program
{
    static void Main(string[] args)
    {
        List<BackupJobJson> json;
        try
        {
            json = JsonManipulator.LoadFile("../../../example.json");
        }
        catch (Exception e)
        {
            ErrorLog(e);
            return;
        }

        List<BackupJob> jobs = json
            .Select(obj =>
            {
                BackupJob job;
                try
                {
                    job = new BackupJob(obj);
                }
                catch (Exception e)
                {
                    ErrorLog(e);
                    return null;
                }
                Console.WriteLine($"Job created with {job.Sources.Count} sources and {job.Targets.Count} targets.");
                return job;
            })
            .Where(job => job is not null)
            .ToList()!;

        Console.WriteLine("Press any key to start backup");
        Console.ReadLine();
        jobs.ForEach(job => job.PerformBackup());
    }

    public static void ErrorLog(Exception e)
    {
        #if DEBUG
            Console.Write($"{e.GetType().Name} occurred [{e.Source}]:\n");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(e.StackTrace);
            Console.ResetColor();
        #endif

        Console.WriteLine(e.Message);
    }
}