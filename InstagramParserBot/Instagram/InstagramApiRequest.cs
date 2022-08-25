using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramParserBot.Tools;

namespace InstagramParserBot.Instagram;

public static class InstagramApiRequest
{
    private static readonly IInstaApi Api = InstagramBuilder.BuildInstaApi().Result;

    private static int _requestCount = 0;
    private static DateTime _firstRequestTime = DateTime.Now;

    private static async Task StartRequest()
    {
        await Delay.WaitOneSecond();

        _requestCount++;

        Console.WriteLine($"[API REQUEST STATUS] Request #{_requestCount}");

        if (_requestCount >= 200 && _firstRequestTime.AddHours(1) > DateTime.Now)
        {
            var delaySeconds = (_firstRequestTime.AddHours(1) - DateTime.Now).Seconds;

            await Delay.Wait(delaySeconds);

            Console.WriteLine($"[API REQUEST STATUS] Request limit, delay: {delaySeconds}");

            _requestCount = 0;
            _firstRequestTime = DateTime.Now;
        }
    }

    public static async Task<InstaUser> GetUserByNickname(string nickname)
    {
        await StartRequest();

        Console.WriteLine($"[API REQUEST STATUS] Searching user '{nickname}'..");

        var user = await Api.UserProcessor.GetUserAsync(nickname);

        if (!user.Succeeded)
        {
            Console.WriteLine($"[API REQUEST ERROR] User '{nickname}' didn't found");
            return null!;
        }

        Console.WriteLine($"[API REQUEST STATUS] User '{user.Value.UserName}' founded");
        return user.Value;
    }

    public static async Task<List<InstagramUserData>> GetUserFollowersListWithKeyWords(long id, string search)
    {
        await StartRequest();

        Console.WriteLine($"[API REQUEST STATUS] Parsing user's '{id}' followers list..");

        var latestMaxId = "";
        var sortedFollowers = new List<InstagramUserData>();

        do
        {
            await StartRequest();

            var allFollowers = await Api.UserProcessor
                .GetUserFollowersByIdAsync(
                    id,
                    PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(latestMaxId),
                    search
                );

            if (allFollowers.Info.ResponseType == ResponseType.RequestsLimit)
            {
                Console.WriteLine($"[API REQUEST STATUS] Requests limit reached, waiting 15 minutes..");
                await Delay.Wait(15 * 60);
            }

            if (!allFollowers.Succeeded)
            {
                Console.WriteLine($"[API REQUEST ERROR] {allFollowers.Info.Message}");
                return FilterFollowers(allFollowers.Value).Result; // return not fully version
            }

            latestMaxId = allFollowers.Value.NextMaxId;
            sortedFollowers.AddRange(FilterFollowers(allFollowers.Value).Result);
        } while (!string.IsNullOrEmpty(latestMaxId));

        Console.WriteLine($"[API REQUEST STATUS] Found {sortedFollowers.Count} users");

        return sortedFollowers;
    }

    private static async Task<List<InstagramUserData>> FilterFollowers(InstaUserShortList users)
    {
        Console.WriteLine("[API REQUEST STATUS] Start sorting with key words..");

        var sortedList = new List<InstagramUserData>();
        var keyWords = new Settings().GetKeyWords();

        foreach (var user in users)
        {
            await StartRequest();

            var userFullDataResult = Api.UserProcessor.GetFullUserInfoAsync(user.Pk).Result;

            if (!userFullDataResult.Succeeded)
            {
                Console.WriteLine($"[API REQUEST ERROR] {userFullDataResult.Info.Message}. Continue sorting..");
                continue;
            }

            var userFullData = userFullDataResult.Value.UserDetail;

            if (keyWords.Any(key =>
                    userFullData.Username.Contains(key)
                    || userFullData.Biography.Contains(key)
                    || userFullData.FullName.Contains(key))
               )
            {
                var publicPhone = userFullData.PublicPhoneNumber.Replace("+7", "8");
                var contactPhone = userFullData.ContactPhoneNumber.Replace("+7", "8");

                if (NumberBase.GetBaseList().Any(number =>
                        publicPhone.Equals(number) || contactPhone.Equals(number))
                   )
                {
                    continue;
                }

                Console.WriteLine("[API REQUEST STATUS] Found one user!!");

                var city = userFullData.CityName;

                sortedList.Add(new InstagramUserData
                {
                    UserName = userFullData.Username,
                    FullName = userFullData.FullName,
                    ContactNumber = contactPhone,
                    PublicNumber = publicPhone,
                    Url = $"https://www.instagram.com/{userFullData.Username}/",
                    City = city,
                    OtherInfo = userFullData
                });
            }
        }

        return sortedList;
    }

    public static async Task<int> GetUserFollowersCount(InstaUser user)
    {
        await StartRequest();

        Console.WriteLine($"[API REQUEST STATUS] Getting user followers count '{user.UserName}'..");

        int count;

        try
        {
            if (user.IsPrivate)
            {
                Console.WriteLine(
                    $"[API REQUEST STATUS] User '{user.UserName}' has private account, can't complete request");
                return 0;
            }

            count = user.FollowersCount;
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[API REQUEST ERROR] Exception: {exception.Message}");
            return -1;
        }

        return count;
    }
}