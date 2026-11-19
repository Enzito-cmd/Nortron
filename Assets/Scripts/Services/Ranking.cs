using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Leaderboards;
using Unity.Services.Leaderboards.Models;

public struct RankEntry
{
    public int Rank;
    public string Name;
    public int Score;
    public bool IsLocal;
}

public static class Ranking
{
    public const string LeaderboardId = "nortron_wins";

    private const char NameSeparator = '#';

    public static async Task SubmitAsync(int wins)
    {
        if (GameServices.IsReady == false)
        {
            return;
        }

        await GameServices.TryRun(() => LeaderboardsService.Instance.AddPlayerScoreAsync(LeaderboardId, wins), "Leaderboard (submit score)");
    }

    public static async Task<List<RankEntry>> FetchTopAsync(int limit)
    {
        if (GameServices.IsReady == false)
        {
            return null;
        }

        List<RankEntry> entries = null;
        GetScoresOptions options = new GetScoresOptions();
        options.Offset = 0;
        options.Limit = limit;

        bool ok = await GameServices.TryRun(async () =>
        {
            LeaderboardScoresPage page = await LeaderboardsService.Instance.GetScoresAsync(LeaderboardId, options);
            entries = ToEntries(page);
        }, "Leaderboard (read table)");

        return ok ? entries : null;
    }

    private static List<RankEntry> ToEntries(LeaderboardScoresPage page)
    {
        List<RankEntry> result = new List<RankEntry>();
        string localId = AuthenticationService.Instance.PlayerId;

        foreach (LeaderboardEntry entry in page.Results)
        {
            RankEntry row = new RankEntry();
            row.Rank = entry.Rank + 1;
            row.Name = CleanName(entry.PlayerName);
            row.Score = (int)entry.Score;
            row.IsLocal = entry.PlayerId == localId;
            result.Add(row);
        }

        return result;
    }

    private static string CleanName(string name)
    {
        string clean = TextFilter.RemoveTags(name);
        int separator = clean.IndexOf(NameSeparator);

        if (separator > 0)
        {
            clean = clean.Substring(0, separator);
        }

        return clean.Length > 0 ? clean : "Player";
    }
}
