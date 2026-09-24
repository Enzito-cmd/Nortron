using System.Linq;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartPad : NetworkBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

    [SerializeField] private BoxCollider padArea;
    [SerializeField] private Renderer padRenderer;
    [SerializeField] private int minPlayers = 2;
    [SerializeField, ColorUsage(false, true)] private Color waitingColor = new Color(0.6f, 0.05f, 0.05f);
    [SerializeField, ColorUsage(false, true)] private Color readyColor = new Color(0f, 3f, 3f);

    [Networked] private NetworkBool IsReady { get; set; }

    private Material padMaterial;
    private bool raceRequested;

    private void Awake()
    {
        padMaterial = padRenderer.material;
        padMaterial.EnableKeyword("_EMISSION");
        padMaterial.SetColor(BaseColorId, Color.black);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority == false)
        {
            return;
        }

        int playerCount = Runner.ActivePlayers.Count();
        bool enoughPlayers = playerCount >= minPlayers;
        IsReady = enoughPlayers;

        if (enoughPlayers == false || raceRequested)
        {
            return;
        }

        LobbyPlayer hostPlayer = LobbyPlayer.Local;

        if (hostPlayer != null && IsOnPad(hostPlayer.transform.position))
        {
            raceRequested = true;
            Runner.LoadScene(SceneRef.FromIndex(NetworkManager.RaceSceneIndex), LoadSceneMode.Single);
        }
    }

    public override void Render()
    {
        Color currentColor = waitingColor;

        if (IsReady)
        {
            currentColor = readyColor;
        }

        padMaterial.SetColor(EmissionColorId, currentColor);
    }

    private bool IsOnPad(Vector3 position)
    {
        Bounds area = padArea.bounds;

        bool insideX = position.x >= area.min.x && position.x <= area.max.x;
        bool insideZ = position.z >= area.min.z && position.z <= area.max.z;

        return insideX && insideZ;
    }
}
