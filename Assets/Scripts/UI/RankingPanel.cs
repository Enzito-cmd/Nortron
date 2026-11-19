using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RankingPanel : MonoBehaviour
{
    [SerializeField] private StandingRow[] rows;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private float refreshSeconds = 30f;

    private bool loading;
    private bool hasData;
    private float nextRefresh;

    private void OnEnable()
    {
        nextRefresh = Time.unscaledTime + refreshSeconds;
        Refresh();
    }

    private void Update()
    {
        if (refreshSeconds > 0f && Time.unscaledTime >= nextRefresh)
        {
            nextRefresh = Time.unscaledTime + refreshSeconds;
            Refresh();
        }
    }

    public void Refresh()
    {
        if (loading == false && rows != null)
        {
            Load();
        }
    }

    private async void Load()
    {
        loading = true;

        if (hasData == false)
        {
            SetStatus("Loading ranking...");
            HideRows();
        }

        await GameServices.Start();
        List<RankEntry> entries = await Ranking.FetchTopAsync(rows.Length);

        loading = false;

        if (this == null)
        {
            return;
        }

        hasData = entries != null;

        if (entries == null)
        {
            SetStatus("Ranking not available (offline or not set up)");
            HideRows();
            return;
        }

        SetStatus(entries.Count == 0 ? "No scores yet" : string.Empty);

        for (int i = 0; i < rows.Length; i++)
        {
            if (rows[i] == null)
            {
                continue;
            }

            if (i < entries.Count)
            {
                string unit = entries[i].Score == 1 ? " win" : " wins";
                rows[i].Show(entries[i].Rank, entries[i].Name, entries[i].Score + unit, entries[i].IsLocal);
            }
            else
            {
                rows[i].Hide();
            }
        }
    }

    private void HideRows()
    {
        foreach (StandingRow row in rows)
        {
            if (row != null)
            {
                row.Hide();
            }
        }
    }

    private void SetStatus(string value)
    {
        if (statusText != null)
        {
            statusText.text = value;
        }
    }
}
