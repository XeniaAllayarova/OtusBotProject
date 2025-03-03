using LinqToDB;
using System.Configuration;
using System.Text;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TgBotProject.Constants;
using TgBotProject.Models;
using TgBotProject.Utilities;
using TgBotProject.Utilits;

namespace TgBotProject.Handlers
{
    static class BotActionHandler
    {
        static string connectionString = ConfigurationManager.AppSettings.Get("SqlConnectionString");
        static Tasks newTask = null;
        static Tasks updateTask = null;
        static string prevCommand = null;
        static string updateField = null;

        delegate bool Compare(Tasks task);

        public static async Task BotOnInlineButtonClick(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            if (prevCommand != null)
            {
                switch (prevCommand)
                {
                    case Constants.Action.Update:
                        await UpdateTask(botClient, cancellationToken, callbackQuery);

                        break;
                    case Constants.Action.Delete:
                        await DeleteTask(botClient, cancellationToken, callbackQuery);

                        break;
                    case Constants.Action.Create:
                        await CreateTask(botClient, cancellationToken, callbackQuery);

                        break;
                }
            }
            else
            {
                await DoneTask(botClient, callbackQuery, cancellationToken);
            }
        }

        public static async Task BotOnMessageReceived(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            if (message.Text == null)
            {
                return;
            }

            Console.WriteLine($"Received {message.Type} '{message.Text}' in {message.Chat}");

            if (message.Type != MessageType.Text)
            {
                return;
            }

            if (message.Text!.StartsWith('/'))
            {
                var command = message.Text.Split(' ')[0];

                switch (command)
                {
                    case Constants.Action.Start:
                        await botClient.SendMessage(message.Chat, "Это бот планировщик задач. Для начала работы создайте задачу", cancellationToken: cancellationToken);

                        Console.WriteLine($"Received {message.Type} '{message.Text}' in {message.Chat}");

                        break;
                    case Constants.Action.Create:
                        await CreateTask(botClient, cancellationToken, message: message);

                        prevCommand = command;

                        break;
                    case Constants.Action.Update:
                        await UpdateTask(botClient, cancellationToken, message: message);

                        prevCommand = command;

                        break;
                    case Constants.Action.Delete:
                        await DeleteTask(botClient, cancellationToken, message: message);

                        prevCommand = command;

                        break;

                    case Constants.Action.ShowAll:
                        await ShowTasks(botClient, message, cancellationToken, (task) => task.Date.Ticks < DateTime.Today.Ticks);

                        break;
                    case Constants.Action.ShowTag:
                        var tagName = message.Text.Split(' ')[1];

                        if (tagName != null)
                        {
                            await ShowTasks(botClient, message, cancellationToken, (task) => task.Date.Ticks < DateTime.Today.Ticks || !task.Tags.Contains(tagName));
                        }
                        else
                        {
                            await botClient.SendMessage(message.Chat, "Тег не был введен", cancellationToken: cancellationToken);
                        }
                        break;
                    default:
                        await MessageGetSuccessfully(botClient, message, cancellationToken);

                        break;
                }

            }
            else if (newTask != null || updateTask != null)
            {
                switch (prevCommand)
                {
                    case Constants.Action.Create:
                        await CreateTask(botClient, cancellationToken, message: message);

                        break;
                    case Constants.Action.Update:
                        await UpdateTask(botClient, cancellationToken, message: message);

                        break;
                    default:
                        await MessageGetSuccessfully(botClient, message, cancellationToken);

                        break;
                }
            }
            else
            {
                await MessageGetSuccessfully(botClient, message, cancellationToken);
            }
        }

        private static async Task ShowTasks(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken, Compare compare)
        {
            using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
            {
                var taskTable = db.GetTable<Tasks>()
                    .Where(task => task.ChatId == message.Chat.Id)
                    .OrderBy(task => task.Date)
                    .ToArray();
                Dictionary<string, List<Tasks>> diary = new();
                StringBuilder result = new StringBuilder();

                foreach (var row in taskTable)
                {
                    if (compare(row))
                    {
                        continue;
                    }

                    var key = row.Date.ToShortDateString();

                    if (!diary.TryAdd(key, new List<Tasks>() { row }))
                    {
                        diary[key].Add(row);
                    }
                }

                foreach (var row in diary)
                {
                    result.AppendLine($"<b>{row.Key}</b>");

                    foreach (var task in row.Value)
                    {
                        var line = $"{task.Date.ToShortTimeString()}, {task.Title}";

                        if (task.Done)
                        {
                            result.AppendLine($"<s>{line}</s>");
                        }
                        else
                        {
                            result.AppendLine(line);
                        }
                    }
                }

                await botClient.SendMessage(message.Chat, $"Ваш список задач:\n{result}", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
            }
        }

        private static async Task CreateTask(ITelegramBotClient botClient, CancellationToken cancellationToken, CallbackQuery callbackQuery = null, Message message = null)
        {

            if (newTask == null)
            {
                newTask = new Tasks(message.Chat.Id);

                await botClient.SendMessage(message.Chat, "Введите название:", cancellationToken: cancellationToken);
            }
            else if (newTask.Title == null)
            {
                newTask.Title = message.Text;

                await botClient.SendMessage(message.Chat, "Введите дату и время начала:", cancellationToken: cancellationToken);
            }
            else if (newTask.Date.Ticks == 0)
            {
                newTask.Date = DateTime.Parse(message.Text);

                ReplyKeyboardMarkup replyKeyboard = new(
                new[]
                {
                   new KeyboardButton[] { "Пропустить" },
                })
                {
                    ResizeKeyboard = true
                };

                await botClient.SendMessage(message.Chat, "Введите теги через запятую:", replyMarkup: replyKeyboard, cancellationToken: cancellationToken);
            }
            else if (newTask.Tags == null)
            {
                if (message.Text == "Пропустить")
                {
                    newTask.Tags = [];
                }
                else
                {
                    newTask.Tags = message.Text.Split(',');
                }

                await botClient.SendMessage(message.Chat, "Напомнить заранее:", replyMarkup: Keyboard.GenerateTimerKeyboard(), cancellationToken: cancellationToken);
            }
            else
            {
                newTask.NotificationTime = Notification.CalculateNotificationTime(message, newTask.Date);

                using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
                {
                    newTask.Id = db.InsertWithInt64Identity(newTask);

                    await Notification.Create(botClient, message.Chat, newTask, cancellationToken: cancellationToken);
                }

                prevCommand = null;
                newTask = null;

                await botClient.SendMessage(message.Chat, $"Задача успешно добавлена!", replyMarkup: new ReplyKeyboardRemove(), cancellationToken: cancellationToken);
            }

        }

        private static async Task DoneTask(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
        {
            var taskId = long.Parse(callbackQuery.Data);

            using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
            {
                var task = db.GetTable<Tasks>().Single(task => task.Id == taskId);
                task.Done = true;

                db.Update(task);

                await botClient.DeleteMessage(callbackQuery.Message.Chat, callbackQuery.Message.MessageId, cancellationToken);
                await botClient.SendMessage(callbackQuery.Message.Chat, $"Задача *{task.Title}* выполнена!", ParseMode.Markdown, cancellationToken: cancellationToken);
            }
        }

        private static async Task DeleteTask(ITelegramBotClient botClient, CancellationToken cancellationToken, CallbackQuery callbackQuery = null, Message message = null)
        {
            using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
            {
                if (callbackQuery != null)
                {
                    var task = db.GetTable<Tasks>().Single(task => task.Id == long.Parse(callbackQuery.Data));
                    db.Delete(task);
                    prevCommand = null;

                    await botClient.DeleteMessage(callbackQuery.Message.Chat, callbackQuery.Message.MessageId, cancellationToken);
                    await botClient.SendMessage(callbackQuery.Message.Chat, "Задача удалена!", cancellationToken: cancellationToken);
                }
                else if (message != null)
                {
                    var taskTable = db.GetTable<Tasks>().Where(task => task.ChatId == message.Chat.Id).ToList();
                    InlineKeyboardMarkup replyKeyboard = Keyboard.GenerateTaskListKeyboard(taskTable);

                    await botClient.SendMessage(message.Chat, "Выберите задачу:", replyMarkup: replyKeyboard, cancellationToken: cancellationToken);
                }
            }
        }

        private static async Task UpdateTask(ITelegramBotClient botClient, CancellationToken cancellationToken, CallbackQuery callbackQuery = null, Message message = null)
        {
            if (message != null)
            {
                if (message.Text != Constants.Action.Update)
                {
                    switch (updateField)
                    {
                        case TaskField.Title:
                            updateTask.Title = message.Text;

                            break;
                        case TaskField.Tags:
                            updateTask.Tags = message.Text.Split(',');

                            break;
                        case TaskField.Date:
                            var notificationDelay = updateTask.Date - updateTask.NotificationTime;

                            updateTask.Date = DateTime.Parse(message.Text);
                            updateTask.NotificationTime = updateTask.Date - notificationDelay;

                            await Notification.Update(botClient, message.Chat, updateTask, cancellationToken: cancellationToken);

                            break;
                        case TaskField.NotificationTime:
                            updateTask.NotificationTime = Notification.CalculateNotificationTime(message, updateTask.Date);

                            await Notification.Update(botClient, message.Chat, updateTask, cancellationToken: cancellationToken);

                            break;
                        default:
                            break;
                    }

                    using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
                    {
                        db.Update(updateTask);
                    }

                    await botClient.SendMessage(message.Chat, "Задача обновлена", cancellationToken: cancellationToken);
                }
                else
                {
                    using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
                    {
                        var taskTable = db.GetTable<Tasks>().Where(task => task.ChatId == message.Chat.Id).ToList();

                        InlineKeyboardMarkup replyKeyboard = Keyboard.GenerateTaskListKeyboard(taskTable);

                        await botClient.SendMessage(message.Chat, "Выберите задачу:", replyMarkup: replyKeyboard, cancellationToken: cancellationToken);
                    }
                }
            }
            else if (callbackQuery != null)
            {
                bool isTaskId = long.TryParse(callbackQuery.Data, out long taskId);

                if (isTaskId)
                {
                    using (var db = new LinqToDB.Data.DataConnection(LinqToDB.ProviderName.PostgreSQL, connectionString))
                    {
                        updateTask = db.GetTable<Tasks>().Single(task => task.Id == long.Parse(callbackQuery.Data));
                    }

                    await botClient.SendMessage(callbackQuery.Message.Chat, "Выберите пункт, который хотите изменить", replyMarkup: Keyboard.GenerateFieldKeyborad(), cancellationToken: cancellationToken);
                }
                else
                {
                    await botClient.DeleteMessage(callbackQuery.Message.Chat, callbackQuery.Message.MessageId, cancellationToken);

                    updateField = callbackQuery.Data;

                    switch (callbackQuery.Data)
                    {
                        case TaskField.Title:
                            await botClient.SendMessage(callbackQuery.Message.Chat, "Введите название:", cancellationToken: cancellationToken);

                            break;
                        case TaskField.Tags:
                            await botClient.SendMessage(callbackQuery.Message.Chat, "Введите теги через запятую:", cancellationToken: cancellationToken);

                            break;
                        case TaskField.Date:
                            await botClient.SendMessage(callbackQuery.Message.Chat, "Введите дату и время начала:", cancellationToken: cancellationToken);

                            break;
                        case TaskField.NotificationTime:
                            await botClient.SendMessage(callbackQuery.Message.Chat, "Напомнить заранее:", replyMarkup: Keyboard.GenerateTimerKeyboard(), cancellationToken: cancellationToken);

                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private static async Task MessageGetSuccessfully(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
        {
            await botClient.SendMessage(message.Chat, $"Сообщение успешно принято", cancellationToken: cancellationToken);
        }
    }
}
