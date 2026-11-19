using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class GameServices
{
    private const int TimeoutSeconds = 10;
    private const string ProfileArgument = "-profile";

    private static Task initialization;

    public static bool IsReady { get; private set; }

    public static bool IsFinished { get; private set; }

    public static string Status { get; private set; } = "Connecting...";

    public static string PlayerId { get; private set; } = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        Start();
    }

    public static Task Start()
    {
        if (initialization == null)
        {
            initialization = Run();
        }

        return initialization;
    }

    public static async Task<bool> TryRun(Func<Task> action, string label)
    {
        try
        {
            Task task = action();
            Task first = await Task.WhenAny(task, Task.Delay(TimeoutSeconds * 1000));

            if (first != task)
            {
                Debug.LogWarning(label + ": timed out");
                return false;
            }

            await task;
            return true;
        }
        catch (Exception exception)
        {
            Debug.LogWarning(label + ": " + exception.Message);
            return false;
        }
    }

    public static string CleanPlayerName(string value)
    {
        string trimmed = TextFilter.RemoveTags(value).Trim().Replace(' ', '_');
        return trimmed.Length > NetworkManager.MaxNameLength ? trimmed.Substring(0, NetworkManager.MaxNameLength) : trimmed;
    }

    public static async void UpdateDisplayName(string value)
    {
        string clean = CleanPlayerName(value);

        if (IsReady == false || clean.Length == 0)
        {
            return;
        }

        await TryRun(() => AuthenticationService.Instance.UpdatePlayerNameAsync(clean), "Player name");
    }

    private static async Task Run()
    {
        if (Application.internetReachability == NetworkReachability.NotReachable)
        {
            Status = "No internet (local mode)";
        }
        else
        {
            bool coreOk = await TryRun(() => UnityServices.InitializeAsync(), "Unity Services");
            bool signedIn = coreOk && await TryRun(SignIn, "Anonymous login");

            IsReady = signedIn;

            if (signedIn)
            {
                PlayerId = AuthenticationService.Instance.PlayerId;
                Status = "Online";
            }
            else
            {
                Status = "No connection to services (local mode)";
            }
        }

        await Task.WhenAll(GameConfig.FetchAsync(), PlayerStats.LoadAsync());
        IsFinished = true;
    }

    private static async Task SignIn()
    {
        string profile = ReadProfileArgument();

        if (profile.Length > 0 && AuthenticationService.Instance.IsSignedIn == false)
        {
            AuthenticationService.Instance.SwitchProfile(profile);
        }

        if (AuthenticationService.Instance.IsSignedIn == false)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }

    private static string ReadProfileArgument()
    {
        string[] arguments = Environment.GetCommandLineArgs();

        for (int i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i] == ProfileArgument)
            {
                return arguments[i + 1];
            }
        }

        return string.Empty;
    }
}
