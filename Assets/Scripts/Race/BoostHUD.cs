using UnityEngine;

public class BoostHUD : MonoBehaviour
{
    private const string IconResource = "BoostIcon";
    private const float SlotHeightFraction = 0.12f;
    private const float IconPadding = 0.12f;
    private const float EmptyAlpha = 0.2f;
    private const float Margin = 24f;
    private const float BarHeight = 6f;

    private Texture2D icon;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (FindFirstObjectByType<BoostHUD>() == null)
        {
            DontDestroyOnLoad(new GameObject("BoostHUD", typeof(BoostHUD)));
        }
    }

    private void Awake()
    {
        icon = Resources.Load<Texture2D>(IconResource);
    }

    private void OnGUI()
    {
        BikeController bike = BikeController.Local;

        if (bike == null || icon == null)
        {
            return;
        }

        bool boosting = bike.IsBoosting;
        float size = Screen.height * SlotHeightFraction;
        float padding = size * IconPadding;

        Rect slot = new Rect(Screen.width - size - Margin, Screen.height - size - Margin, size, size);
        Rect iconArea = new Rect(slot.x + padding, slot.y + padding, size - padding * 2f, size - padding * 2f);

        Color previous = GUI.color;

        GUI.Box(slot, GUIContent.none);

        GUI.color = new Color(1f, 1f, 1f, bike.HasBoost || boosting ? 1f : EmptyAlpha);
        GUI.DrawTexture(iconArea, icon, ScaleMode.ScaleToFit, true);

        if (boosting)
        {
            GUI.color = Color.cyan;
            GUI.DrawTexture(new Rect(slot.x, slot.yMax + BarHeight, size * bike.BoostFill, BarHeight), Texture2D.whiteTexture);
        }

        GUI.color = previous;
    }
}
