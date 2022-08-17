using Microsoft.Extensions.Configuration;

namespace InstagramParserBot.Tools;

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

    public bool ProxyEntered => _settings.ProxyHost != "";

    public string GetToken() => _settings.Token;
    public string GetUserName() => _settings.UserName;
    public string GetUserPassword() => _settings.UserPassword;
    public string GetProxyHost() => _settings.ProxyHost;
    public string GetProxyPort() => _settings.ProxyPort;
    public string GetProxyUserName() => _settings.ProxyUserName;
    public string GetProxyUserPassword() => _settings.ProxyUserPassword;
    public string[] GetKeyWords() => _settings.KeyWords;
}

public class BotSettings
{
    public string Token { get; set; }
    public string UserName { get; set; }
    public string UserPassword { get; set; }
    public string ProxyHost { get; set; }
    public string ProxyPort { get; set; }
    public string ProxyUserName { get; set; }
    public string ProxyUserPassword { get; set; }
    public string[] KeyWords { get; set; }
}