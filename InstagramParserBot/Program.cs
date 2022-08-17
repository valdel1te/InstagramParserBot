using InstagramApiSharp;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Classes.Models;
using InstagramParserBot.Bot;
using InstagramParserBot.Instagram;

//await Bot.Start();

await InstagramBuilder.Start();

var insta = InstagramBuilder.GetInstaApi();

Console.WriteLine(insta.AccountProcessor.SetBiographyAsync("говножуй, делаю дела"));

var list = new InstaUserShortList();
var latestMaxId = "";

var danil = await insta.UserProcessor.GetUserAsync("dima_master_dristun");
// if (!danil.Succeeded)
//     if (danil.Info.ResponseType == ResponseType.ChallengeRequired)
//     {
//         var challengeData = await insta.GetLoggedInChallengeDataInfoAsync();
//         var acceptChallenge = await insta.AcceptChallengeAsync(); 
//     }

danil = await insta.UserProcessor.GetUserAsync("dima_master_dristun");

var pk = danil.Value.Pk;

while (latestMaxId != null)
    bebra();

Console.WriteLine(list.Count);
Console.WriteLine("---danya milokhin's followers---");

foreach (var user in list)
{
    Console.WriteLine(user.UserName + " / " + user.FullName);
}

Console.ReadLine();

async void bebra()
{
    Console.WriteLine("ПОШЛА ЖАРА");

    var followers = await insta.UserProcessor
        .GetUserFollowersByIdAsync(pk, PaginationParameters.MaxPagesToLoad(1).StartFromMaxId(latestMaxId));

    latestMaxId = followers.Value.NextMaxId;

    if (followers.Succeeded)
        list.AddRange(followers.Value);
}