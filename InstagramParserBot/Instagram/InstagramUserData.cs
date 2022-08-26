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

    public override string ToString()
    {
        var result = "";

        if (string.IsNullOrEmpty(City))
            result += $"{FullName}\n";
        else
            result += $"{FullName}, {City}\n";

        if (string.IsNullOrEmpty(ContactNumber))
            result += $"{Url}\n";
        else
            result += $"{ContactNumber}\n{Url}\n";

        return result;
    }
}