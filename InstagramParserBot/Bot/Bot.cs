using InstagramParserBot.Bot.Telegram;
using InstagramParserBot.Tools;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

namespace InstagramParserBot.Bot;

public static class Bot
{
    public static async Task Start()
    {
        var bot = new TelegramBotClient(new Settings().GetToken());
        var me = await bot.GetMeAsync();

        using var cancellationToken = new CancellationTokenSource();

        var receiverOptions = new ReceiverOptions()
        {
            AllowedUpdates = Array.Empty<UpdateType>(),
            ThrowPendingUpdates = true,
        };

        bot.StartReceiving(updateHandler: Handlers.HandleUpdateAsync,
            pollingErrorHandler: Handlers.PollingErrorHandler,
            receiverOptions: receiverOptions,
            cancellationToken: cancellationToken.Token);

        Console.WriteLine($"[BOT STATUS] Start listening for @{me.Username}");
        
        //cancellationToken.Cancel();
    }
}