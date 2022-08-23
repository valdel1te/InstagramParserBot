using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace InstagramParserBot.Bot.Telegram;

public static class Handlers
{
    public static Task PollingErrorHandler(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"[TELEGRAM API ERROR]: [{apiRequestException.ErrorCode}]\n[{apiRequestException.Message}]",
            
            _ => exception.ToString()
        };
        
        Console.WriteLine($"[BOT ERROR] {errorMessage}");
        return Task.CompletedTask;
    }

    public static async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken
    )
    {
        var handler = update.Type switch
        {
            UpdateType.Message => Events.OnMessageReceivedEvent(botClient, update.Message!),
            UpdateType.CallbackQuery => Events.OnCallbackQueryReceivedEvent(botClient, update.CallbackQuery!)
        };

        try
        {
            await handler;
        }
        catch (Exception exception)
        {
            await PollingErrorHandler(botClient, exception, cancellationToken);
        }
    }
}