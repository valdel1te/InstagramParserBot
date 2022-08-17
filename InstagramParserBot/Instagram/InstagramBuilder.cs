using System.Net;
using InstagramApiSharp.API;
using InstagramApiSharp.API.Builder;
using InstagramApiSharp.Classes;
using InstagramApiSharp.Logger;
using InstagramParserBot.Tools;

namespace InstagramParserBot.Instagram;

public static class InstagramBuilder
{
    private static IInstaApi _instaApi;

    public static IInstaApi GetInstaApi() => _instaApi;

    public static async Task<bool> Start()
    {
        var settings = new Settings();

        var userSession = new UserSessionData
        {
            UserName = settings.GetUserName(),
            Password = settings.GetUserPassword()
        };

        try
        {
            if (settings.ProxyEntered)
            {
                Console.WriteLine($"[INSTAGRAM API STATUS] Building with proxy");

                _instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    .UseHttpClientHandler(ConnectProxy(settings))
                    .SetRequestDelay(RequestDelay.FromSeconds(0, 1))
                    .Build();
            }
            else
            {
                Console.WriteLine($"[INSTAGRAM API STATUS] Building without proxy");

                _instaApi = InstaApiBuilder.CreateBuilder()
                    .SetUser(userSession)
                    .UseLogger(new DebugLogger(LogLevel.Exceptions))
                    .SetRequestDelay(RequestDelay.FromSeconds(0, 1))
                    .Build();
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine($"[INSTAGRAM API STATUS] Error, exception: {exception}");
            return false;
        }

        if (!_instaApi.IsUserAuthenticated)
        {
            Console.WriteLine($"[INSTAGRAM API STATUS] Logging as {userSession.UserName}");

            await _instaApi.SendRequestsBeforeLoginAsync();
            await Task.Delay(5000);

            var loginResult = await _instaApi.LoginAsync();

            if (loginResult.Succeeded)
            {
                Console.WriteLine($"[INSTAGRAM API STATUS] Connected as {userSession.UserName}");
                await _instaApi.SendRequestsAfterLoginAsync();
            }
            else
            {
                if (loginResult.Value == InstaLoginResult.ChallengeRequired)
                    await _instaApi.GetChallengeRequireVerifyMethodAsync();
                
                Console.WriteLine($"[INSTAGRAM API STATUS] Error, unable to login: {loginResult.Info.Message}");
                return false;
            }
        }


        Console.WriteLine("[INSTAGRAM API STATUS] Successfully built");
        return true;
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

        var httpClientHandler = new HttpClientHandler
        {
            Proxy = proxy
        };

        httpClientHandler.ServerCertificateCustomValidationCallback =
            (sender, cert, clain, sslPolicyErrors) => true;

        return httpClientHandler;
    }
}