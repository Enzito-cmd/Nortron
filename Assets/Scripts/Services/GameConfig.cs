using System;
using System.Threading.Tasks;
using Unity.Services.RemoteConfig;
using UnityEngine;

public static class GameConfig
{
    public const string CatchUpEnabledKey = "catchup_enabled";
    public const string CatchUpMinKey = "catchup_min_kmh";
    public const string CatchUpBaseKey = "catchup_base_kmh";
    public const string CatchUpMaxKey = "catchup_max_kmh";
    public const string CatchUpSecondsKey = "catchup_full_seconds";
    public const string LapsKey = "laps_to_win";
    public const string BoostBonusKey = "boost_bonus_kmh";
    public const string MaintenanceKey = "maintenance_mode";
    public const string MaintenanceMessageKey = "maintenance_message";
    public const string MinVersionKey = "min_version";

    private const float RefreshSeconds = 20f;
    private const string DefaultMaintenanceMessage = "The game is under maintenance. Try again later.";

    private static float lastFetchTime = -1000f;
    private static bool fetching;

    public static bool IsRemote { get; private set; }

    public static string SourceLabel => IsRemote ? "Remote Config" : "default values";

    public static bool Maintenance => GetBool(MaintenanceKey, false);

    public static string MaintenanceMessage
    {
        get
        {
            string value = GetString(MaintenanceMessageKey, DefaultMaintenanceMessage);
            return value.Length > 0 ? value : DefaultMaintenanceMessage;
        }
    }

    public static string MinVersion => GetString(MinVersionKey, string.Empty);

    public static bool IsVersionAllowed
    {
        get
        {
            if (Version.TryParse(MinVersion, out Version required) == false || Version.TryParse(Application.version, out Version current) == false)
            {
                return true;
            }

            return current >= required;
        }
    }

    public static async Task FetchAsync()
    {
        if (fetching)
        {
            return;
        }

        fetching = true;
        lastFetchTime = Time.realtimeSinceStartup;

        if (GameServices.IsReady)
        {
            AppAttributes attributes = new AppAttributes();
            attributes.appVersion = Application.version;

            bool ok = await GameServices.TryRun(() => RemoteConfigService.Instance.FetchConfigsAsync(new UserAttributes(), attributes), "Remote Config");
            IsRemote = IsRemote || ok;
        }

        fetching = false;
    }

    public static void RefreshIfStale()
    {
        if (GameServices.IsFinished && Time.realtimeSinceStartup - lastFetchTime >= RefreshSeconds)
        {
            _ = FetchAsync();
        }
    }

    public static float GetFloat(string key, float fallback, float min, float max)
    {
        float value = IsRemote ? RemoteConfigService.Instance.appConfig.GetFloat(key, fallback) : fallback;

        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            value = fallback;
        }

        return Mathf.Clamp(value, min, max);
    }

    public static int GetInt(string key, int fallback, int min, int max)
    {
        int value = IsRemote ? RemoteConfigService.Instance.appConfig.GetInt(key, fallback) : fallback;
        return Mathf.Clamp(value, min, max);
    }

    public static bool GetBool(string key, bool fallback)
    {
        return IsRemote ? RemoteConfigService.Instance.appConfig.GetBool(key, fallback) : fallback;
    }

    public static string GetString(string key, string fallback)
    {
        string value = IsRemote ? RemoteConfigService.Instance.appConfig.GetString(key, fallback) : fallback;
        return value == null ? fallback : value.Trim();
    }

    public struct UserAttributes
    {
    }

    public struct AppAttributes
    {
        public string appVersion;
    }
}
