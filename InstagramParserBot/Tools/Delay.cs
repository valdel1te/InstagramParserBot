namespace InstagramParserBot.Tools;

public static class Delay
{
    public static async Task WaitOneSecond() => await Task.Delay(1000);

    public static async Task Wait(int seconds) => await Task.Delay(seconds * 1000);
}