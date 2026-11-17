using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private int minPlayers = 2;

    private bool requested;

    private void Awake()
    {
        button.onClick.AddListener(Play);
    }

    private void Update()
    {
        NetworkManager manager = NetworkManager.Instance;
        bool visible = manager != null && manager.IsHost && requested == false && CanPlay();

        if (button.gameObject.activeSelf != visible)
        {
            button.gameObject.SetActive(visible);
        }
    }

    private bool CanPlay()
    {
        return LobbyPlayer.All.Count >= minPlayers && TrackPad.GetHighestCount() > 0;
    }

    private void Play()
    {
        NetworkManager manager = NetworkManager.Instance;

        if (requested || manager == null || manager.IsHost == false || CanPlay() == false)
        {
            return;
        }

        TrackPad pad = TrackPad.PickWinner();

        if (pad == null)
        {
            return;
        }

        requested = manager.LoadRace(pad.SceneBuildIndex);
    }
}
