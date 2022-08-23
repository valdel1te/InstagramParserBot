using InstagramParserBot.Instagram;
using Telegram.Bot.Types;

namespace InstagramParserBot.Bot;

public static class UserStatement
{
    private static readonly Dictionary<long, UserMessageStatement> UserStatementsMap = new();

    public static bool UserAlreadyAdded(long id) => UserStatementsMap.ContainsKey(id);
    public static UserMessageStatement GetStatement(long id) => UserStatementsMap[id];

    public static void AddUser(long id) => UserStatementsMap.Add(id, new UserMessageStatement());
    public static void RemoveUser(long id) => UserStatementsMap.Remove(id);

    public static void EndWorkWithFollowersList(long id) =>
        UserStatementsMap[id].Status = Status.WaitingWithNumbersBase;

    public static void StartWorkWithFollowersList(long id) =>
        UserStatementsMap[id].Status = Status.WorkingWithFollowersList;

    /* развить идею этого варианта будет хорошо только в случае желания сделать проект максимально удобным для множества пользователей
    public static void ConfirmAddedNumbersBase(long id) =>
        UserStatementsMap[id].Status = Statement.WaitingWithNumbersBase;
    */
}

public enum Status
{
    WaitingWithNumbersBase,
    WorkingWithFollowersList
    //WaitingWithoutNumbersBase
}

public sealed class UserMessageStatement
{
    public Status Status { get; set; }
    public int MessageId { get; set; }
    public List<InstagramUserData> UserDataList { get; set; }
    public int NextUserDataIndex { get; set; }

    public UserMessageStatement()
    {
        Status = Status.WaitingWithNumbersBase;
        MessageId = 0;
        UserDataList = new List<InstagramUserData>();
        NextUserDataIndex = 0;
    }

    public void IncrementIndex() => ++NextUserDataIndex;
    public void DecrementIndex() => --NextUserDataIndex;
}