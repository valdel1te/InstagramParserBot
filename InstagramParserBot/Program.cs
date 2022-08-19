using System.Text;
using InstagramParserBot.Bot;
using InstagramParserBot.Instagram;

Console.OutputEncoding = Encoding.Unicode; // enable russian lang and other

await Bot.Start();

await InstagramBuilder.BuildInstaApi();

// var vika = InstagramApiRequest.GetUserByNickname("vika_srgv").Result;
//
// Console.WriteLine(vika.FullName + " " + vika.Pk);
//
// var list = InstagramApiRequest.GetUserFollowersList(vika.Pk).Result;
//
// for (var i = 0; i < list.Count; i++)
// {
//     Console.WriteLine($"{i + 1} | {list[i].UserName}");
// }

Console.ReadLine();