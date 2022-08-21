using InstagramParserBot.Instagram;
using Org.BouncyCastle.Crypto.Tls;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace InstagramParserBot.Bot.Telegram;

public static class Events
{
    public static async Task OnMessageReceivedEvent(ITelegramBotClient botClient, Message message)
    {
        if (message.Text is not { } messageText)
            return;

        if (message.From!.IsBot)
            return;

        var split = messageText.Split(" ");

        if (split.Length is > 3 or < 2)
        {
            await SendErrorMessage(botClient, message);
            return;
        }

        var action = split[0] switch
        {
            "/followers" => StartParseFollowers(botClient, message, split[1], split[2]),
            "/f" => StartParseFollowers(botClient, message, split[1], split[2]),
            _ => SendDefaultAnswer(botClient, message)
        };

        var messageSent = await action;
        Console.WriteLine($"[BOT STATUS] Sent message to {message.From.Username}");

        static async Task<Message> StartParseFollowers(
            ITelegramBotClient botClient,
            Message message,
            string nickname,
            string searchWord
        )
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
                    text: "Не нашел такого, то ли аккаунт приватный, то ли мне отдохнуть надо минут 15, то ли ты шизик"
                );
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Нашел!! {search.UserName}, {search.Pk}.\nПробиваю подписчиков"
            );

            var followers = InstagramApiRequest
                .GetUserFollowersListWithKeyWords(search.Pk, searchWord)
                .Result;

            //var tempOutput = followers.Aggregate("", (current, follower) => current + (follower.UserName + "\n"));

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Полный список подходящих аккаунтов!"
            );

            foreach (var user in followers)
            {
                var text =
                    $"1. Пользователь *{user.UserName}*\n" +
                    $"2. Полное имя: `{user.FullName}`\n" +
                    $"3. Контактный номер: `{user.ContactNumber}`\n" +
                    $"4. Публичный номер: `{user.PublicNumber}`\n" +
                    $"5. Город: `{user.City}\n`" +
                    "\nПри наличии нужды имзенить некоторые свойства, нажмите 1-5 соответственно";

                InlineKeyboardMarkup inlineKeyboard = new(new[]
                {
                    new[] // url
                    {
                        InlineKeyboardButton.WithUrl(
                            text: "Открыть аккаунт через инстаграм",
                            url: user.Url
                        )
                    },
                    new[] // edit data
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "1",
                            callbackData: "editNickName"
                        ),
                        InlineKeyboardButton.WithCallbackData(
                            text: "2",
                            callbackData: "editFullName"
                        ),
                        InlineKeyboardButton.WithCallbackData(
                            text: "3",
                            callbackData: "editContactNumber"
                        ),
                        InlineKeyboardButton.WithCallbackData(
                            text: "4",
                            callbackData: "editPublicNumber"
                        ),
                        InlineKeyboardButton.WithCallbackData(
                            text: "5",
                            callbackData: "editCity"
                        )
                    },
                    new[] // continue
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Перейти к следующему",
                            callbackData: "nextUser"
                        )
                    },
                    new[] // remove from list
                    {
                        InlineKeyboardButton.WithCallbackData(
                            text: "Удалить аккаунт из списка",
                            callbackData: "removeUser"
                        )
                    }
                });

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: text,
                    replyMarkup: inlineKeyboard,
                    parseMode: ParseMode.Markdown
                );
            }

            return await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Если список готов, нажмите кнопку ниже, чтобы создать Word-документ",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "Создать документ",
                        callbackData: "printWordDocument"
                    )
                })
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