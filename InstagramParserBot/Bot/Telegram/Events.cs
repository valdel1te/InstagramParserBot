using InstagramParserBot.Instagram;
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

        if (!UserStatement.UserAlreadyAdded(message.Chat.Id))
            UserStatement.AddUser(message.Chat.Id);

        var userStatement = UserStatement.GetStatement(message.Chat.Id);
        
        if (userStatement.Status == Status.WorkingWithFollowersList)
        {
            var index = Convert.ToInt32(messageText);
            if (index >= 0 && index < userStatement.UserDataList.Count)
            {
                userStatement.NextUserDataIndex = index;
                await SendFollowerInfoMessage(userStatement, message.Chat.Id, botClient);
                return;
            }
        }

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
            var userMessageStatement = UserStatement.GetStatement(message.Chat.Id);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Начинаю искать заданного пользователя.."
            );

            var search = await InstagramApiRequest.GetUserByNickname(nickname);

            if (search == null!)
            {
                return await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Не нашел такого, то ли мне отдохнуть надо минут 15, то ли ты шизик"
                );
            }

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"Нашел!! USERNAME: {search.UserName}, ID: {search.Pk}.\nПробиваю подписчиков"
            );

            var followers = InstagramApiRequest
                .GetUserFollowersListWithKeyWords(search.Pk, searchWord)
                .Result;

            //var tempOutput = followers.Aggregate("", (current, follower) => current + (follower.UserName + "\n"));

            var botMessage = await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Полный список подходящих аккаунтов!"
            );

            userMessageStatement.UserDataList = followers;
            userMessageStatement.Status = Status.WorkingWithFollowersList;
            userMessageStatement.MessageId = botMessage.MessageId;

            await botClient.SendTextMessageAsync(
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

            return await SendFollowerInfoMessage(userMessageStatement, message.Chat.Id, botClient);
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

    public static async Task OnCallbackQueryReceivedEvent(ITelegramBotClient botClient, CallbackQuery callbackQuery)
    {
        var chatId = callbackQuery.Message!.Chat.Id;
        var userMessageStatement = UserStatement.GetStatement(chatId);

        if (userMessageStatement.Status != Status.WorkingWithFollowersList)
            return;

        var action = callbackQuery.Data switch
        {
            "nextUser" => SendFollowerInfoMessage(userMessageStatement, chatId, botClient, moveForward: true),
            "pastUser" => SendFollowerInfoMessage(userMessageStatement, chatId, botClient, moveBack: true)
        };

        await action;
    }

    private static async Task<Message> SendFollowerInfoMessage(
        UserMessageStatement userMessageStatement,
        long chatId,
        ITelegramBotClient botClient,
        bool moveForward = false,
        bool moveBack = false
    )
    {
        if (moveForward)
            userMessageStatement.IncrementIndex();
        if (moveBack)
            userMessageStatement.DecrementIndex();
        
        var nextUserDataIndex = userMessageStatement.NextUserDataIndex;
        var userDataList = userMessageStatement.UserDataList;
        var editMessageId = userMessageStatement.MessageId;

        if (userDataList.Count == nextUserDataIndex || nextUserDataIndex < 0)
        {
            userMessageStatement.NextUserDataIndex = -1;

            return await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: editMessageId,
                text: "Достигнута граница списка!",
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "Начать с начала",
                        callbackData: "nextUser"
                    )
                })
            );
        }

        var user = userDataList[nextUserDataIndex];

        var text =
            $"№{nextUserDataIndex + 1}\n" +
            $"1. Пользователь *{user.UserName}*\n" +
            $"2. Полное имя: `{user.FullName}`\n" +
            $"3. Контактный номер: `{user.ContactNumber}`\n" +
            $"4. Публичный номер: `{user.PublicNumber}`\n" +
            $"5. Город: `{user.City}\n`" +
            "\n*При наличии нужды имзенить некоторые свойства, нажмите 1-5 соответственно*\n" +
            "_Чтобы выбрать конкретный номер аккаунта в списке, отправьте ниже соответствующий индекс_";

        var inlineKeyboard = CreateInlineReplyMarkupForFollowersList(user.Url);

        return await botClient.EditMessageTextAsync(
            chatId: chatId,
            messageId: editMessageId,
            text: text,
            replyMarkup: inlineKeyboard,
            parseMode: ParseMode.Markdown
        );
    }

    private static InlineKeyboardMarkup CreateInlineReplyMarkupForFollowersList(string url) =>
        new(new[]
        {
            new[] // url
            {
                InlineKeyboardButton.WithUrl(
                    text: "Открыть аккаунт через инстаграм",
                    url: url
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
            new[] // go forward or back
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "Перейти к следующему",
                    callbackData: "nextUser"
                ),
                InlineKeyboardButton.WithCallbackData(
                    text: "Вернуться к предыдущему",
                    callbackData: "pastUser"
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
}