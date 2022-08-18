using System.Net;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using InstagramParserBot.Tools;

namespace InstagramParserBot.Instagram;

public static class InstagramBuilder
{
    private static IInstaApi _instaApi = null!;
    private static HttpClientHandler _httpClientHandler = null!;

    public static async Task<IInstaApi> BuildInstaApi()
    {
        if (_instaApi == null!)
            await Start();
        
        return _instaApi!;
    }

    private static async Task Start()
    {
        var settings = new Settings();

        var userSession = new UserSessionData
        {
            UserName = settings.GetUserName(),
            Password = settings.GetUserPassword()
        };

        var delay = RequestDelay.FromSeconds(2, 4);

        try
        {
            if (settings.ProxyEntered)
            {
                Console.WriteLine($"[INSTAGRAM API STATUS] Building with proxy");

                _instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    //.UseLogger(new DebugLogger(LogLevel.Request))
                    //.UseLogger(new DebugLogger(LogLevel.Response))
                    .UseHttpClientHandler(ConnectProxy(settings))
                    .SetRequestDelay(delay)
                    .Build();
            }
            else
            {
                Console.WriteLine($"[INSTAGRAM API STATUS] Building without proxy");

                _instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    .UseLogger(new DebugLogger(LogLevel.Request))
                    .SetRequestDelay(delay)
                    .Build();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[INSTAGRAM API STATUS] Error, exception: {exception}");
            return;
        }

        if (!_instaApi.IsUserAuthenticated)
        {
            Console.WriteLine($"[INSTAGRAM API STATUS] Logging as {userSession.UserName}");

            await _instaApi.SendRequestsBeforeLoginAsync();

            await Delay.WaitFiveSeconds();

            var loginResult = await _instaApi.LoginAsync();

            if (loginResult.Succeeded && userSession.UserName == settings.GetUserName())
            {
                Console.WriteLine($"[INSTAGRAM API STATUS] Connected as {userSession.UserName}");
                await Delay.WaitFiveSeconds();
                await _instaApi.SendRequestsAfterLoginAsync();
            }
            else
            {
                if (loginResult.Value == InstaLoginResult.ChallengeRequired)
                {
                    var challenge = await _instaApi.GetChallengeRequireVerifyMethodAsync();

                    if (challenge.Succeeded)
                        Console.WriteLine("[INSTAGRAM API ERROR] Blocked access, need reopen account");
                    else
                        Console.WriteLine($"[INSTAGRAM API ERROR] {challenge.Info.Message}");
                }

                Console.WriteLine($"[INSTAGRAM API ERROR] Unable to login: {loginResult.Info.Message}");
                return;
            }
        }


        Console.WriteLine(
            $"[INSTAGRAM API STATUS] Successfully built -> " +
            $"User: [{_instaApi.UserProcessor.GetCurrentUserAsync().Result.Value.Pk}] " +
            $"[{_instaApi.UserProcessor.GetCurrentUserAsync().Result.Value.UserName}]"
        );
    }

    private static HttpClientHandler ConnectProxy(Settings settings)
    {
        Console.WriteLine(
            $"[INSTAGRAM API STATUS] Connecting with proxy {settings.GetProxyHost()}:{settings.GetProxyPort()}");

        var proxy = new WebProxy
        {
            Address = new Uri($"http://{settings.GetProxyHost()}:{settings.GetProxyPort()}"),
            BypassProxyOnLocal = false,
            UseDefaultCredentials = false,
            Credentials = new NetworkCredential
            {
                UserName = settings.GetProxyUserName(),
                Password = settings.GetProxyUserPassword()
            }
        };

        _httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy
        };

        _httpClientHandler.ServerCertificateCustomValidationCallback =
            (sender, cert, chain, sslPolicyErrors) => true;

        return _httpClientHandler;
    }
}