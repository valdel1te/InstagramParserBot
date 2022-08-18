using InstagramParserBot.Instagram;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace InstagramParserBot.Bot.Telegram;

public static class Events
{
    public static async Task OnMessageReceivedEvent(ITelegramBotClient botClient, Message message)
    {
        if (message.Text is not { } messageText)
            return;

        var split = messageText.Split("\\s+");

        if (split.Length < 2)
            await SendErrorMessage(botClient, message);

        var action = split[0] switch
        {
            "/followers" => StartParseFollowers(botClient, message, split[1]),
            "/f" => StartParseFollowers(botClient, message, split[1]),
            _ => SendDefaultAnswer(botClient, message)
        };

        await action;

        static async Task<Message> StartParseFollowers(ITelegramBotClient botClient, Message message, string nickname)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Начинаю искать заданного пользователя.."
            );

            var search = await InstagramApiRequest.GetUserByNickname(nickname);

            if (search == null!)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Не нашел такого, то ли приватный, то ли мне отдохнуть надо, то ли ты шиз"
                );
            }
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Нашел!! {search.UserName}, {search.Pk}.\nПробиваю подписчиков"
            );

            var followers = await InstagramApiRequest.GetUserFollowersList(search.Pk);
            
            //todo а дальше что ё
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Полный список подходящих аккаунтов!"
            );
        }

        static async Task<Message> SendDefaultAnswer(ITelegramBotClient botClient, Message message)
        {
            const string text = "Не разговаривай так со мной, приятель";
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: text
            );
        }

        static async Task<Message> SendErrorMessage(ITelegramBotClient botClient, Message message)
        {
            const string text = "Ничего не понял";
            
            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: text
            );
        }
    } 
}