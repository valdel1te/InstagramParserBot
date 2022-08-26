using InstagramParserBot.Instagram;
using InstagramParserBot.Tools;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.InputFiles;
using Telegram.Bot.Types.ReplyMarkups;
using File = System.IO.File;

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

        if (userStatement.Status != Status.WaitingWithNumbersBase)
        {
            if (userStatement.Status == Status.WorkingWithFollowersList)
            {
                if (int.TryParse(messageText, out var index))
                {
                    if (index >= 0 && index < userStatement.UserDataList.Count)
                    {
                        userStatement.NextUserDataIndex = index - 1;
                        await SendFollowerInfoMessage(userStatement, message.Chat.Id, botClient);
                    }
                }

                await DeleteMessage(botClient, message);
                return;
            }

            if (userStatement.Status == Status.EditingFollowersUserName)
                userStatement.UserDataList[userStatement.NextUserDataIndex].UserName = messageText;

            if (userStatement.Status == Status.EditingFollowersFullName)
                userStatement.UserDataList[userStatement.NextUserDataIndex].FullName = messageText;

            if (userStatement.Status == Status.EditingFollowersPublicNumber)
                userStatement.UserDataList[userStatement.NextUserDataIndex].PublicNumber = messageText;

            if (userStatement.Status == Status.EditingFollowersContactNumber)
                userStatement.UserDataList[userStatement.NextUserDataIndex].ContactNumber = messageText;

            if (userStatement.Status == Status.EditingFollowersCity)
                userStatement.UserDataList[userStatement.NextUserDataIndex].City = messageText;

            await SendFollowerInfoMessage(userStatement, message.Chat.Id, botClient);
            await DeleteMessage(botClient, message);

            userStatement.Status = Status.WorkingWithFollowersList;

            return;
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

        await action;
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
            "pastUser" => SendFollowerInfoMessage(userMessageStatement, chatId, botClient, moveBack: true),
            "getShortInfoList" => SendShortInfoFollowersList(userMessageStatement, chatId, botClient),
            "editNickName" => EditUserInfo(userMessageStatement, chatId, botClient, Status.EditingFollowersUserName),
            "editFullName" => EditUserInfo(userMessageStatement, chatId, botClient, Status.EditingFollowersFullName),
            "editContactNumber" => EditUserInfo(userMessageStatement, chatId, botClient,
                Status.EditingFollowersContactNumber),
            "editPublicNumber" => EditUserInfo(userMessageStatement, chatId, botClient,
                Status.EditingFollowersPublicNumber),
            "editCity" => EditUserInfo(userMessageStatement, chatId, botClient, Status.EditingFollowersCity),
            "removeUser" => RemoveUserFromList(userMessageStatement, chatId, botClient),
            "printWordDocument" => SendDocumentAndEndProcess(userMessageStatement, chatId, botClient)
        };

        await action;

        static async Task<Message> SendDocumentAndEndProcess(
            UserMessageStatement userMessageStatement,
            long chatId,
            ITelegramBotClient botClient
        )
        {
            var dataList = userMessageStatement.UserDataList;

            MicrosoftOfficeService.SendWordDocument(dataList);

            Console.WriteLine($"[BOT STATUS] PRINTING WORD DOCUMENT");

            var documentName = $"accounts{DateTime.Today:ddMM}.docx";

            await using Stream streamPath =
                File.OpenRead(@$"{Directory.GetCurrentDirectory()}/{documentName}");

            var newNumbers = dataList.Select(user => user.ContactNumber).ToList();
            NumberBase.AppendBase(newNumbers);
            
            UserStatement.RemoveUser(chatId);
            
            await botClient.SendDocumentAsync(
                chatId: chatId,
                document: new InputOnlineFile(
                    content: streamPath,
                    fileName: documentName
                )
            );

            return await botClient.SendTextMessageAsync(
                chatId: chatId,
                text:
                $"Составлен список из {dataList.Count} пользователей!" +
                $"\nЗабавный факт: если кто-то действительно пользуется этим ботом, то с него {dataList.Count * 4} руб"
            );
        }

        static async Task<Message> SendShortInfoFollowersList(
            UserMessageStatement userMessageStatement,
            long chatId,
            ITelegramBotClient botClient
        )
        {
            var text = "";
            for (var i = 0; i < userMessageStatement.UserDataList.Count; i++)
                text += $"{i + 1}. {userMessageStatement.UserDataList[i].UserName}\n";

            userMessageStatement.DecrementIndex();

            return await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: userMessageStatement.MessageId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "Вернуться к подробному списку",
                        callbackData: "nextUser"
                    )
                })
            );
        }

        static async Task<Message> EditUserInfo(
            UserMessageStatement userMessageStatement,
            long chatId,
            ITelegramBotClient botClient,
            Status editStatus
        )
        {
            userMessageStatement.Status = editStatus;
            var username = userMessageStatement.UserDataList[userMessageStatement.NextUserDataIndex].UserName;

            var text = editStatus switch
            {
                Status.EditingFollowersUserName => $"УКАЖИТЕ НОВЫЙ НИКНЕЙМ ДЛЯ `{username}`",
                Status.EditingFollowersFullName => $"УКАЖИТЕ НОВОЕ ПОЛНОЕ ИМЯ ДЛЯ `{username}`",
                Status.EditingFollowersPublicNumber => $"УКАЖИТЕ НОВЫЙ ПУБЛИЧНЫЙ НОМЕР ДЛЯ `{username}`",
                Status.EditingFollowersContactNumber => $"УКАЖИТЕ НОВЫЙ КОНТАКТНЫЙ НОМЕР ДЛЯ `{username}`",
                Status.EditingFollowersCity => $"УКАЖИТЕ НОВЫЙ ГОРОД ДЛЯ `{username}`",
                _ => "Что-то пошло не так, отмените действие"
            };

            return await botClient.EditMessageTextAsync(
                chatId: chatId,
                messageId: userMessageStatement.MessageId,
                text: text,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData(
                        text: "Отменить",
                        callbackData: "nextUser"
                    )
                }),
                parseMode: ParseMode.Markdown
            );
        }

        static async Task RemoveUserFromList(
            UserMessageStatement userMessageStatement,
            long chatId,
            ITelegramBotClient botClient
        )
        {
            var index = userMessageStatement.NextUserDataIndex;
            var deletedUser = userMessageStatement.UserDataList[index];
            userMessageStatement.UserDataList.RemoveAt(index);

            Console.WriteLine($"[BOT STATUS] Removed user from list: {deletedUser.UserName}");

            await SendFollowerInfoMessage(userMessageStatement, chatId, botClient);
        }
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
            "\n*При наличии нужды изменить некоторые свойства, нажмите 1-5 соответственно*\n" +
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

    private static async Task DeleteMessage(ITelegramBotClient botClient, Message message) =>
        await botClient.DeleteMessageAsync(chatId: message.Chat.Id, messageId: message.MessageId);

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
            new[] // go back or forward
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "◀️Вернуться к предыдущему",
                    callbackData: "pastUser"
                ),
                InlineKeyboardButton.WithCallbackData(
                    text: "Перейти к следующему ▶️",
                    callbackData: "nextUser"
                )
            },
            new[] // remove from list
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "Удалить аккаунт из списка",
                    callbackData: "removeUser"
                )
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    text: "Вывести краткий список аккаунтов",
                    callbackData: "getShortInfoList"
                )
            }
        });
}