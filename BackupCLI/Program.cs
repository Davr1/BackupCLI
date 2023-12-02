using BackupCLI.Backup;
using BackupCLI.Helpers;

namespace BackupCLI;

public class Program
{
    public static readonly CustomLogger Logger = new("latest.log");

    static void Main(string[] args)
    {
        if (!JsonManipulator.TryLoadFile("../../../example.json", out List<BackupJobJson>? json)) return;

        List<BackupJob> jobs = json
            .Select(obj =>
            {
                BackupJob.TryCreate(obj, out BackupJob? job);
                return job;
            })
            .Where(job => job is not null)
            .ToList();

        jobs.ForEach(job => job.PerformBackup());
    }
}