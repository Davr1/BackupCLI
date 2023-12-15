using BackupCLI.Backup;
using Quartz;
using Quartz.Impl;
using System.Diagnostics;

namespace BackupCLI;

/// <summary>
/// Utility class for scheduling backup jobs with the quartz.net library.
/// </summary>
public static class Scheduler
{
    private class SchedulerJob : IJob
    {
        /// <summary>
        /// Executes the given job and logs the time it took to complete.
        /// </summary>
        public Task Execute(IJobExecutionContext context)
        {
            var watch = new Stopwatch();
            var job = (BackupJob)context.JobDetail.JobDataMap["job"];

            watch.Start();

            Program.Logger.Info($"Performing {job.Method.ToString().ToLower()} backup: {{ {string.Join(", ", job.Sources)} }} -> {{ {string.Join(", ", job.Targets)} }}");

            try
            {
                job.PerformBackup();
            }
            catch (Exception e)
            {
                Program.Logger.Error(e);
            }

            watch.Stop();
            Program.Logger.Info($@"Took {watch.Elapsed:h\:mm\:ss}");

            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Schedules the given backup jobs
    /// </summary>
    /// <param name="jobs">List of jobs to schedule - must have a valid <see cref="BackupJob.Timing"/> property</param>
    public static async Task SetupCronJobs(List<BackupJob> jobs)
    {
        IScheduler scheduler = await StdSchedulerFactory.GetDefaultScheduler();

        await scheduler.Start();

        foreach (var job in jobs)
        {
            IJobDetail schedulerJob = JobBuilder.Create<SchedulerJob>().Build();

            ITrigger trigger = TriggerBuilder
                .Create()
                .WithSchedule(CronScheduleBuilder.CronSchedule(job.Timing))
                .StartNow()
                .Build();

            schedulerJob.JobDataMap["job"] = job;

            await scheduler.ScheduleJob(schedulerJob, trigger);
        }
    }
}
