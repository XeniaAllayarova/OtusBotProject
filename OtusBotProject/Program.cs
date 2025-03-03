using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using TgBotProject.Handlers;
using TgBotProject.Utilities;

namespace TgBotProject
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var botClient = BotClient.Initialize();
            var receiverOptions = new ReceiverOptions
            {
                AllowedUpdates = [UpdateType.Message, UpdateType.CallbackQuery],
                DropPendingUpdates = true,
            };
            var handler = new UpdateHandler();

            using (var cts = new CancellationTokenSource())
            {
                botClient.StartReceiving(handler, receiverOptions, cts.Token);

                var me = await botClient.GetMe(cts.Token);

                Console.WriteLine($"{me.FirstName} запущен!");

                while (true)
                {
                    var command = Console.ReadLine();

                    if (command == "A")
                    {
                        cts.Cancel();

                        break;
                    }
                    else
                    {
                        Console.WriteLine(me);
                    }
                }
            }
        }
    }
}
