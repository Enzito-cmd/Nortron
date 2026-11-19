using TMPro;
using UnityEngine;

public class MenuInfo : MonoBehaviour
{
    [SerializeField] private TMP_Text versionText;
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

        SetText(versionText, "v" + Application.version);
        SetText(statusText, GameServices.Status + " | Config: " + GameConfig.SourceLabel);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
