using BackupCLI.Backup;
using Quartz;
using Quartz.Impl;
using System.Diagnostics;

namespace BackupCLI.Helpers;

public static class Scheduler
{
    private class SchedulerJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var watch = new Stopwatch();
            var job = (BackupJob)context.JobDetail.JobDataMap["job"];

            watch.Start();

            Program.Logger.Info($"Performing {job.Method.ToString().ToLower()} backup: {{ {string.Join(", ", job.Sources)} }} -> {{ {string.Join(", ", job.Targets)} }}");

            job.PerformBackup();

            watch.Stop();
            Program.Logger.Info($"Took {watch.ElapsedMilliseconds} ms");

            return Task.CompletedTask;
        }
    }

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
