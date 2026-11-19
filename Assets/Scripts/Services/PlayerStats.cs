using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using UnityEngine;

public static class PlayerStats
{
    private const string NameKey = "name";
    private const string ColorKey = "color";
    private const string RacesKey = "races";
    private const string WinsKey = "wins";
    private const int SaveColorDelayMilliseconds = 1500;

    private static Task loading;
    private static bool cloudOk;
    private static int colorVersion;

    public static bool Loaded { get; private set; }

    public static bool Online => cloudOk;

    public static string SavedName { get; private set; } = string.Empty;

    public static int SavedColor { get; private set; } = -1;

    public static int Races { get; private set; }

    public static int Wins { get; private set; }

    public static Task LoadAsync()
    {
        if (loading == null)
        {
            loading = Load();
        }

        return loading;
    }

    public static string FormatTime(float seconds)
    {
        if (seconds <= 0f)
        {
            return "--:--";
        }

        int minutes = Mathf.FloorToInt(seconds / 60f);
        float rest = seconds - minutes * 60f;
        return minutes + ":" + rest.ToString("00.00", CultureInfo.InvariantCulture);
    }

    public static async void RecordRace(bool won)
    {
        await GameServices.Start();

        Races++;

        if (won)
        {
            Wins++;
        }

        if (cloudOk == false)
        {
            return;
        }

        Dictionary<string, object> data = new Dictionary<string, object>();
        data[RacesKey] = Races;
        data[WinsKey] = Wins;

        bool saved = await GameServices.TryRun(() => CloudSaveService.Instance.Data.Player.SaveAsync(data), "Cloud Save (save result)");

        if (saved && won)
        {
            await Ranking.SubmitAsync(Wins);
        }
    }

    public static async void SaveName(string value)
    {
        string clean = value.Trim();

        if (clean.Length == 0 || clean == SavedName)
        {
            return;
        }

        SavedName = clean;
        GameServices.UpdateDisplayName(clean);
        await Save(NameKey, clean, "Cloud Save (save name)");
    }

    public static async void SaveColor(int index)
    {
        if (index == SavedColor)
        {
            return;
        }

        SavedColor = index;
        int version = ++colorVersion;

        await Task.Delay(SaveColorDelayMilliseconds);

        if (version == colorVersion)
        {
            await Save(ColorKey, index, "Cloud Save (save color)");
        }
    }

    private static async Task Save(string key, object value, string label)
    {
        await GameServices.Start();

        if (cloudOk == false)
        {
            return;
        }

        Dictionary<string, object> data = new Dictionary<string, object>();
        data[key] = value;

        await GameServices.TryRun(() => CloudSaveService.Instance.Data.Player.SaveAsync(data), label);
    }

    private static async Task Load()
    {
        if (GameServices.IsReady == false)
        {
            Loaded = true;
            return;
        }

        HashSet<string> keys = new HashSet<string> { NameKey, ColorKey, RacesKey, WinsKey };
        Dictionary<string, Item> data = null;

        bool ok = await GameServices.TryRun(async () => data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys), "Cloud Save (load)");

        if (ok && data != null)
        {
            cloudOk = true;
            SavedName = Read(data, NameKey, string.Empty).Trim();
            SavedColor = Read(data, ColorKey, -1);
            Races = Mathf.Max(0, Read(data, RacesKey, 0));
            Wins = Mathf.Max(0, Read(data, WinsKey, 0));

            if (SavedColor < 0 || SavedColor >= PlayerIdentity.ColorCount)
            {
                SavedColor = -1;
            }

            if (SavedName.Length > NetworkManager.MaxNameLength)
            {
                SavedName = SavedName.Substring(0, NetworkManager.MaxNameLength);
            }
        }

        Loaded = true;
    }

    private static T Read<T>(Dictionary<string, Item> data, string key, T fallback)
    {
        if (data.TryGetValue(key, out Item item) == false)
        {
            return fallback;
        }

        try
        {
            return item.Value.GetAs<T>();
        }
        catch (Exception)
        {
            return fallback;
        }
    }
}
