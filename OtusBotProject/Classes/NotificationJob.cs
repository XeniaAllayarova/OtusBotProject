using Quartz;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace TgBotProject.Classes
{
    class NotificationJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            JobKey key = context.JobDetail.Key;
            JobDataMap dataMap = context.JobDetail.JobDataMap;

            string message = dataMap.GetString("message");
            Chat chat = (Chat)dataMap["chat"];
            ITelegramBotClient botClient = (ITelegramBotClient)dataMap["botClient"];
            long taskId = dataMap.GetLong("taskId");
            CancellationToken cancellationToken = (CancellationToken)dataMap["cancellationToken"];

            InlineKeyboardMarkup doneButton = new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Готово", taskId.ToString())
                    }
                });

            await botClient.SendMessage(chat, message, parseMode: ParseMode.Markdown, replyMarkup: doneButton, cancellationToken: cancellationToken);

        }
    }
}
