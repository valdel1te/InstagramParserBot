using InstagramApiSharp.Classes.Models;

namespace InstagramParserBot.Instagram;

public class InstagramUserData
{
    public string UserName { get; set; }
    public string FullName { get; set; }
    public string ContactNumber { get; set; }
    public string PublicNumber { get; set; }
    public string Url { get; set; }
    public string City { get; set; }
    public InstaUserInfo OtherInfo { get; set; }
}