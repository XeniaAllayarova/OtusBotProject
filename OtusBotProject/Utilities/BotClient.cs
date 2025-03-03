using System.Configuration;
using Telegram.Bot;

namespace TgBotProject.Utilities
{
    class BotClient
    {
        public static TelegramBotClient Initialize()
        {
            string botToken = ConfigurationManager.AppSettings.Get("BotToken");

            return new TelegramBotClient(botToken);
        }
    }
}
