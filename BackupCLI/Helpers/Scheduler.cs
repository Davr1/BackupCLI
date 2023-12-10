using BackupCLI.Backup;
using Quartz;
using Quartz.Impl;

namespace BackupCLI.Helpers;

public static class Scheduler
{
    private class SchedulerJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            var job = (BackupJob)context.JobDetail.JobDataMap["job"];

            job.PerformBackup();

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
                .WithCronSchedule(job.Timing.CronExpressionString)
                .StartNow()
                .Build();

            schedulerJob.JobDataMap["job"] = job;

            await scheduler.ScheduleJob(schedulerJob, trigger);
        }
    }
}