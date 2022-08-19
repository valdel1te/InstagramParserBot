namespace InstagramParserBot.Tools;

public static class Delay
{
    public static async Task WaitThreeSeconds() => await Task.Delay(3000);

    public static async Task Wait(int seconds) => await Task.Delay(seconds * 1000);
}