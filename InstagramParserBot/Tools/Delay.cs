namespace InstagramParserBot.Tools;

public static class Delay
{
    public static async Task WaitFiveSeconds() => await Task.Delay(5000);

    public static async Task Wait(int seconds) => await Task.Delay(seconds * 1000);
}