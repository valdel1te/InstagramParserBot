using InstagramParserBot.Instagram;

namespace InstagramParserBot.Bot;

public static class UserStatement
{
    private static readonly Dictionary<long, UserMessageStatement> UserStatementsMap = new();

    public static bool UserAlreadyAdded(long id) => UserStatementsMap.ContainsKey(id);
    public static UserMessageStatement GetStatement(long id) => UserStatementsMap[id];

    public static void AddUser(long id) => UserStatementsMap.Add(id, new UserMessageStatement());
    public static void RemoveUser(long id) => UserStatementsMap.Remove(id);
}

public enum Status
{
    WaitingWithNumbersBase,
    WorkingWithFollowersList,
    EditingFollowersUserName,
    EditingFollowersFullName,
    EditingFollowersPublicNumber,
    EditingFollowersContactNumber,
    EditingFollowersCity
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