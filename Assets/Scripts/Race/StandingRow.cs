using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StandingRow : MonoBehaviour
{
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text lapText;
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = new Color(0f, 0f, 0f, 0.5f);
    [SerializeField] private Color localColor = new Color(0f, 0.9f, 1f, 0.35f);

    public void Show(int position, string playerName, string lap, bool isLocal)
    {
        gameObject.SetActive(true);

        SetText(positionText, position.ToString());
        SetText(nameText, playerName);
        SetText(lapText, lap);

        if (background != null)
        {
            background.color = isLocal ? localColor : normalColor;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
