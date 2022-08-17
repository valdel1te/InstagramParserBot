using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstagramParserBot.Bot.Telegram;

public static class Events
{
    public static async Task OnMessageReceivedEvent(ITelegramBotClient botClient, Message message)
    {
        if (message.Text is not { } messageText)
            return;

        var action = messageText.Split("\\s+")[0] switch
        {
            _ => SendDefaultAnswer(botClient, message)
        };

        await action;

        static async Task<Message> SendDefaultAnswer(ITelegramBotClient botClient, Message message)
        {
            const string text = "Не разговаривай так со мной, приятель";
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: text
            );
        }
    } 
}