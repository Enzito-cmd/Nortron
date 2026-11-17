using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TrackPad : MonoBehaviour
{
    private const string TrackSuffix = "Racetrack";

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly List<TrackPad> pads = new List<TrackPad>();

    [SerializeField] private int sceneBuildIndex;
    [SerializeField] private BoxCollider padArea;
    [SerializeField] private Renderer padRenderer;
    [SerializeField] private TMP_Text label;
    [SerializeField] private bool faceCamera = true;
    [SerializeField, ColorUsage(false, true)] private Color idleColor = new Color(0.6f, 0.05f, 0.05f);
    [SerializeField, ColorUsage(false, true)] private Color chosenColor = new Color(0f, 3f, 3f);

    private Material padMaterial;
    private string trackName;
    private int shownCount = -1;

    public int SceneBuildIndex => sceneBuildIndex;

    public static int GetHighestCount()
    {
        int highest = 0;

        for (int i = 0; i < pads.Count; i++)
        {
            highest = Mathf.Max(highest, pads[i].CountPlayers());
        }

        return highest;
    }

    public static TrackPad PickWinner()
    {
        int highest = GetHighestCount();

        if (highest == 0)
        {
            return null;
        }

        int ties = 0;

        for (int i = 0; i < pads.Count; i++)
        {
            if (pads[i].CountPlayers() == highest)
            {
                ties++;
            }
        }

        int pick = Random.Range(0, ties);

        for (int i = 0; i < pads.Count; i++)
        {
            if (pads[i].CountPlayers() != highest)
            {
                continue;
            }

            if (pick == 0)
            {
                return pads[i];
            }

            pick--;
        }

        return null;
    }

    public int CountPlayers()
    {
        Bounds area = padArea.bounds;
        int count = 0;

        for (int i = 0; i < LobbyPlayer.All.Count; i++)
        {
            LobbyPlayer player = LobbyPlayer.All[i];

            if (player == null)
            {
                continue;
            }

            Vector3 position = player.transform.position;
            bool insideX = position.x >= area.min.x && position.x <= area.max.x;
            bool insideZ = position.z >= area.min.z && position.z <= area.max.z;

            if (insideX && insideZ)
            {
                count++;
            }
        }

        return count;
    }

    private void Awake()
    {
        trackName = BuildTrackName();

        if (padRenderer != null)
        {
            padMaterial = padRenderer.material;
            padMaterial.EnableKeyword("_EMISSION");
            padMaterial.SetColor(BaseColorId, Color.black);
        }
    }

    private void OnEnable()
    {
        pads.Add(this);
    }

    private void OnDisable()
    {
        pads.Remove(this);
    }

    private void Update()
    {
        int count = CountPlayers();

        if (count != shownCount)
        {
            shownCount = count;
            ShowLabel(count);
        }

        if (padMaterial != null)
        {
            bool leading = count > 0 && count == GetHighestCount();
            padMaterial.SetColor(EmissionColorId, leading ? chosenColor : idleColor);
        }

        if (faceCamera && label != null && Camera.main != null)
        {
            label.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void ShowLabel(int count)
    {
        if (label == null)
        {
            return;
        }

        label.text = trackName + "\n" + count + (count == 1 ? " player" : " players");
    }

    private string BuildTrackName()
    {
        string path = SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex);

        if (string.IsNullOrEmpty(path))
        {
            return "Track " + sceneBuildIndex;
        }

        string sceneName = System.IO.Path.GetFileNameWithoutExtension(path);

        if (sceneName.EndsWith(TrackSuffix))
        {
            return sceneName.Substring(0, sceneName.Length - TrackSuffix.Length);
        }

        return sceneName;
    }
}
