using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace TgBotProject.Handlers
{
    internal class UpdateHandler : IUpdateHandler
    {
        public Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
        {
            var errorMessage = exception.Message;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(errorMessage);
            Console.ForegroundColor = ConsoleColor.White;

            return Task.CompletedTask;
        }

        public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            try
            {
                switch (update.Type)
                {
                    case UpdateType.Message:
                        await BotActionHandler.BotOnMessageReceived(botClient, update.Message, cancellationToken);

                        break;

                    case UpdateType.CallbackQuery:
                        await BotActionHandler.BotOnInlineButtonClick(botClient, update.CallbackQuery, cancellationToken);

                        break;
                    default:
                        break;
                }

            }
            catch (Exception ex)
            {
                await HandleErrorAsync(botClient, ex, HandleErrorSource.PollingError, cancellationToken);
            }
        }
    }
}