using Telegram.Bot.Types.ReplyMarkups;
using TgBotProject.Models;

namespace TgBotProject.Utilits
{
    static class Keyboard
    {
        static public InlineKeyboardMarkup GenerateTaskListKeyboard(List<Tasks> tasks)
        {
            List<Tasks> undoneTasks = tasks.Where(task => !task.Done).ToList();
            int length = undoneTasks.Count;
            int rows = length % 3 != 0 ? length / 3 + 1 : length / 3;
            List<List<InlineKeyboardButton>> buttons = new List<List<InlineKeyboardButton>>();

            for (int i = 0; i < length; i++)
            {
                int row = i / 3;
                int column = i % 3;

                if (row == buttons.Count)
                {
                    buttons.Add(new List<InlineKeyboardButton>());
                }

                buttons[row].Add(new InlineKeyboardButton(undoneTasks[i].Title, undoneTasks[i].Id.ToString()));
            }

            return new InlineKeyboardMarkup(buttons);
        }

        static public ReplyKeyboardMarkup GenerateTimerKeyboard()
        {
            return new(
                new[]
                {
                   new KeyboardButton[] { "15 минут ", "30 минут", "1 час"},
                   new KeyboardButton[] { "2 часа", "6 часов", "12 часов" }

                })
            {
                ResizeKeyboard = true
            };
        }

        static public InlineKeyboardMarkup GenerateFieldKeyborad()
        {
            return new(
                new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Заголовок", "title"),
                        InlineKeyboardButton.WithCallbackData("Теги", "tags")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Дата", "date"),
                        InlineKeyboardButton.WithCallbackData("Время уведомления", "notification_time")
                    }
                }
                );
        }
    }
}
