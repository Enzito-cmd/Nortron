using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;

public class RaceHUD : MonoBehaviour
{
    private const float TextRefreshSeconds = 0.1f;
    private const float PingRefreshSeconds = 0.5f;
    private const float SpeedSmoothing = 10f;
    private const int GoodPingMs = 60;
    private const int BadPingMs = 120;

    [Header("Status")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Color statusColor = Color.white;
    [SerializeField] private Color winColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private Color loseColor = new Color(1f, 0.3f, 0.3f);

    [Header("Race")]
    [SerializeField] private TMP_Text lapText;
    [SerializeField] private StandingRow[] rows;

    [Header("Ping")]
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Color goodPingColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField] private Color mediumPingColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color badPingColor = new Color(1f, 0.3f, 0.3f);

    [Header("Speedometer")]
    [SerializeField] private RectTransform needle;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private float maxSpeed = 100f;
    [SerializeField] private float minAngle = 120f;
    [SerializeField] private float maxAngle = -120f;

    private float shownSpeed;
    private int shownSpeedValue = -1;
    private float nextTextRefresh;
    private float nextPingRefresh;

    private void Update()
    {
        RaceManager raceManager = RaceManager.Instance;

        if (raceManager == null)
        {
            return;
        }

        UpdateSpeedometer();
        UpdatePing(raceManager);

        if (Time.time < nextTextRefresh)
        {
            return;
        }

        nextTextRefresh = Time.time + TextRefreshSeconds;

        UpdateStatus(raceManager);
        UpdateLap(raceManager);
        UpdateStandings(raceManager);
    }

    private void UpdateSpeedometer()
    {
        BikeController bike = BikeController.Local;
        float target = bike != null ? bike.SpeedKmh : 0f;

        shownSpeed = Mathf.Lerp(shownSpeed, target, 1f - Mathf.Exp(-SpeedSmoothing * Time.deltaTime));

        if (needle != null)
        {
            float fraction = Mathf.Clamp01(shownSpeed / maxSpeed);
            needle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(minAngle, maxAngle, fraction));
        }

        int rounded = Mathf.RoundToInt(shownSpeed);

        if (rounded != shownSpeedValue)
        {
            shownSpeedValue = rounded;
            SetText(speedText, rounded.ToString());
        }
    }

    private void UpdatePing(RaceManager raceManager)
    {
        if (pingText == null || Time.time < nextPingRefresh)
        {
            return;
        }

        NetworkRunner runner = raceManager.Runner;

        if (runner == null)
        {
            return;
        }

        nextPingRefresh = Time.time + PingRefreshSeconds;

        int ping = Mathf.RoundToInt((float)(runner.GetPlayerRtt(runner.LocalPlayer) * 1000.0));

        SetText(pingText, "Ping: " + ping + " ms" + (runner.IsServer ? " (host)" : string.Empty));
        pingText.color = ping < GoodPingMs ? goodPingColor : ping < BadPingMs ? mediumPingColor : badPingColor;
    }

    private void UpdateStatus(RaceManager raceManager)
    {
        if (statusText == null)
        {
            return;
        }

        switch (raceManager.State)
        {
            case RaceState.Waiting:
                ShowStatus("Waiting for players...", statusColor);
                break;

            case RaceState.Countdown:
                ShowStatus(Mathf.CeilToInt(raceManager.GetCountdownSecondsRemaining()).ToString(), statusColor);
                break;

            case RaceState.Finished:
                bool won = raceManager.IsLocalPlayerWinner();
                ShowStatus(won ? "YOU WON" : "YOU LOST", won ? winColor : loseColor);
                break;

            default:
                ShowStatus(string.Empty, statusColor);
                break;
        }
    }

    private void UpdateLap(RaceManager raceManager)
    {
        BikeController bike = BikeController.Local;

        if (bike == null)
        {
            return;
        }

        int lap = Mathf.Min(bike.CurrentLap + 1, raceManager.LapsToWin);
        SetText(lapText, "LAP " + lap + "/" + raceManager.LapsToWin);
    }

    private void UpdateStandings(RaceManager raceManager)
    {
        if (rows == null)
        {
            return;
        }

        List<BikeController> standings = raceManager.GetStandings();

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null)
            {
                continue;
            }

            if (i >= standings.Count)
            {
                rows[i].Hide();
                continue;
            }

            BikeController bike = standings[i];
            int lap = Mathf.Min(bike.CurrentLap + 1, raceManager.LapsToWin);

            rows[i].Show(i + 1, bike.PlayerName, "V" + lap + "/" + raceManager.LapsToWin, bike == BikeController.Local);
        }
    }

    private void ShowStatus(string text, Color color)
    {
        SetText(statusText, text);
        statusText.color = color;
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
