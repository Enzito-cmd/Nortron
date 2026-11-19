using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResultsPanel : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private StandingRow[] rows;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private float refreshSeconds = 0.25f;

    private float nextRefresh;

    private void Awake()
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        RaceManager raceManager = RaceManager.Instance;
        bool finished = raceManager != null && raceManager.State == RaceState.Finished;

        if (panel != null && panel.activeSelf != finished)
        {
            panel.SetActive(finished);
        }

        if (finished == false || Time.unscaledTime < nextRefresh)
        {
            return;
        }

        nextRefresh = Time.unscaledTime + refreshSeconds;

        ShowWinner(raceManager);
        ShowRows(raceManager);
    }

    private void ShowWinner(RaceManager raceManager)
    {
        if (winnerText == null)
        {
            return;
        }

        List<BikeController> standings = raceManager.GetStandings();
        string winner = standings.Count > 0 ? standings[0].PlayerName : "-";
        winnerText.text = "Winner: " + winner + "   " + PlayerStats.FormatTime(raceManager.WinnerSeconds);
    }

    private void ShowRows(RaceManager raceManager)
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
            string detail = i == 0 ? PlayerStats.FormatTime(raceManager.WinnerSeconds) : "L" + Mathf.Min(bike.CurrentLap + 1, raceManager.LapsToWin) + "/" + raceManager.LapsToWin;

            rows[i].Show(i + 1, bike.PlayerName, detail, bike == BikeController.Local);
        }
    }
}
