using InstagramParserBot.Bot.Telegram;
using InstagramParserBot.Bot.Tools;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

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

Console.WriteLine($"Start listening for @{me.Username}");
Console.ReadLine();

//cancellationToken.Cancel();