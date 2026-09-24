using TMPro;
using UnityEngine;

public class RaceHUD : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI statusText;

    private void Update()
    {
        RaceManager raceManager = RaceManager.Instance;

        if (raceManager == null || statusText == null)
        {
            return;
        }

        statusText.text = BuildStatusText(raceManager);
    }

    private string BuildStatusText(RaceManager raceManager)
    {
        switch (raceManager.State)
        {
            case RaceState.Waiting:
                return "Esperando jugadores...";

            case RaceState.Countdown:
                return Mathf.CeilToInt(raceManager.GetCountdownSecondsRemaining()).ToString();

            case RaceState.Racing:
                if (BikeController.Local == null)
                {
                    return string.Empty;
                }

                int lap = BikeController.Local.CurrentLap;
                int position = raceManager.GetPosition(BikeController.Local);
                return position + "°/" + raceManager.BikeCount + " - Vuelta " + Mathf.Min(lap + 1, raceManager.LapsToWin) + "/" + raceManager.LapsToWin;

            case RaceState.Finished:
                return raceManager.IsLocalPlayerWinner() ? "GANASTE" : "PERDISTE";

            default:
                return string.Empty;
        }
    }
}
