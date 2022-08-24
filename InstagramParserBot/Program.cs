using System.Text;
using InstagramParserBot.Bot;
using InstagramParserBot.Instagram;

Console.OutputEncoding = Encoding.Unicode; // enable russian lang and other

await Bot.Start();
await InstagramBuilder.BuildInstaApi();

Console.ReadLine();