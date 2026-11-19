using TMPro;
using UnityEngine;

public class StatsPanel : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text racesText;
    [SerializeField] private TMP_Text winsText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float refreshSeconds = 0.5f;

    private float nextRefresh;

    private void OnEnable()
    {
        nextRefresh = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshSeconds;

        SetText(nameText, GetPlayerName());
        SetText(racesText, "Races: " + PlayerStats.Races);
        SetText(winsText, "Wins: " + PlayerStats.Wins);
        SetText(statusText, BuildStatus());
    }

    private static string GetPlayerName()
    {
        PlayerIdentity identity = PlayerIdentity.Local;
        return identity != null ? identity.DisplayName : PlayerStats.SavedName;
    }

    private static string BuildStatus()
    {
        if (PlayerStats.Loaded == false)
        {
            return "Loading...";
        }

        return PlayerStats.Online ? string.Empty : "Progress not saved (offline)";
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
