using BackupCLI.Backup;
using Microsoft.Extensions.Logging;

namespace BackupCLI;

public class Program
{
    public static readonly ILogger Logger = new CustomLogger("latest.log");

    static void Main(string[] args)
    {

        //todo: rewrite this entire file
        List<BackupJobJson> json;
        try
        {
            json = JsonManipulator.LoadFile("../../../example.json");
        }
        catch (Exception e)
        {
            Error(e);
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
                    Console.WriteLine("Job creation skipped:");
                    Error(e);
                    return null;
                }
                Console.WriteLine($"Job created with {job.Sources.Count} sources and {job.Targets.Count} targets.");
                return job;
            })
            .Where(job => job is not null)
            .ToList()!;

        var watch = System.Diagnostics.Stopwatch.StartNew();
        jobs.ForEach(job => job.PerformBackup());
        watch.Stop();
        Console.WriteLine($"Took {watch.ElapsedMilliseconds} ms");
    }

    public static void Error(Exception e) 
        => Logger.LogError(0, e, e.Message);
}