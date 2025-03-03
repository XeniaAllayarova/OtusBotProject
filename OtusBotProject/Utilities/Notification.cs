using Quartz;
using Quartz.Impl;
using Telegram.Bot;
using Telegram.Bot.Types;
using TgBotProject.Classes;
using TgBotProject.Models;

namespace TgBotProject.Utilities
{
    class Notification
    {
        static StdSchedulerFactory factory = new StdSchedulerFactory();
        static IScheduler scheduler;

        public static async Task Create(ITelegramBotClient botClient, Chat chat, Tasks task, CancellationToken cancellationToken)
        {
            var message = $"Напоминаю про задачу: *{task.Title}* в *{task.Date.ToShortTimeString()}*";

            scheduler = await factory.GetScheduler(cancellationToken);

            await scheduler.Start(cancellationToken);

            IJobDetail job = JobBuilder.Create<NotificationJob>()
                .WithIdentity(task.Id.ToString())
                .UsingJobData("message", message)
                .UsingJobData("taskId", task.Id)
                .Build();

            job.JobDataMap["botClient"] = botClient;
            job.JobDataMap["chat"] = chat;
            job.JobDataMap["cancellationToken"] = cancellationToken;

            ITrigger trigger = TriggerBuilder.Create()
             .WithIdentity(task.Id.ToString())
               .StartAt(task.NotificationTime)
               .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        public static async Task Update(ITelegramBotClient botClient, Chat chat, Tasks task, CancellationToken cancellationToken)
        {
            var message = $"Напоминаю про задачу: *{task.Title}* в *{task.Date.ToShortTimeString()}*";

            if (scheduler == null)
            {
                scheduler = await factory.GetScheduler(cancellationToken);

                await scheduler.Start(cancellationToken);
            }

            await scheduler.UnscheduleJob(new TriggerKey(task.Id.ToString()), cancellationToken);

            IJobDetail job = JobBuilder.Create<NotificationJob>()
                .WithIdentity(task.Id.ToString())
                .UsingJobData("message", message)
                .UsingJobData("taskId", task.Id)
                .Build();

            job.JobDataMap["botClient"] = botClient;
            job.JobDataMap["chat"] = chat;
            job.JobDataMap["cancellationToken"] = cancellationToken;

            ITrigger trigger = TriggerBuilder.Create()
             .WithIdentity(task.Id.ToString())
               .StartAt(task.NotificationTime)
               .Build();

            await scheduler.ScheduleJob(job, trigger, cancellationToken);
        }

        public static DateTime CalculateNotificationTime(Message message, DateTime taskTime)
        {
            var splitTime = message.Text.Split(' ');
            DateTime notificationTime = taskTime;

            if (splitTime[1].Contains("час"))
            {
                return notificationTime.AddHours(-1 * int.Parse(splitTime[0]));
            }
            else
            {
                return notificationTime.AddMinutes(-1 * int.Parse(splitTime[0]));
            }
        }
    }
}
