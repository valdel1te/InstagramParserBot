using Microsoft.Extensions.Configuration;

namespace InstagramParserBot.Bot.Tools;

public sealed class Settings
{
    private readonly BotSettings _settings;

    public Settings()
    {
        var configurationBuilder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        _settings = configurationBuilder.GetSection("BotSettings").Get<BotSettings>()!;
    }

    public string GetToken() => _settings.Token;
}

public class BotSettings
{
    public string Token { get; set; }
}