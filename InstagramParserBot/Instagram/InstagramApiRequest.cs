using InstagramApiSharp;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramParserBot.Tools;
using Microsoft.VisualBasic.CompilerServices;

namespace InstagramParserBot.Instagram;

public static class InstagramApiRequest
{
    private static readonly IInstaApi Api = InstagramBuilder.BuildInstaApi().Result;

    private static int _requestCount = 0;
    private static DateTime _firstRequestTime = DateTime.Now;

    private static async Task StartRequest()
    {
        await Delay.WaitFiveSeconds();

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

    public static async Task<InstaUserShortList> GetUserFollowersList(long id)
    {
        await StartRequest();

        Console.WriteLine($"[API REQUEST STATUS] Parsing user's '{id}' followers list..");
        
        var latestMaxId = "";
        var followersList = new InstaUserShortList();

        do
        {

            //await StartRequest();

            var followers = await Api.UserProcessor
                .GetUserFollowersByIdAsync(id, PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(latestMaxId));

            if (followers.Info.ResponseType == ResponseType.RequestsLimit)
            {
                Console.WriteLine($"[API REQUEST STATUS] Requests limit reached, waiting 15 minutes..");
                await Delay.Wait(15 * 60);
            }

            if (!followers.Succeeded)
            {
                Console.WriteLine($"[API REQUEST ERROR] {followers.Info.Message}");
                return followersList; // return not fully version
            }

            latestMaxId = followers.Value.NextMaxId;
            followersList.AddRange(followers.Value);

        } while (string.IsNullOrEmpty(latestMaxId));

        Console.WriteLine($"[API REQUEST STATUS] Parsed {followersList.Count} users");
        
        return followersList;
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
                Console.WriteLine($"[API REQUEST STATUS] User '{user.UserName}' has private account, can't complete request");
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