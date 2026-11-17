using UnityEngine;

public class StandingsToggle : MonoBehaviour
{
    [SerializeField] private RectTransform panel;
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab;
    [SerializeField] private Vector2 slideDirection = Vector2.left;
    [SerializeField] private float slideMargin = 60f;
    [SerializeField] private float slideSeconds = 0.25f;
    [SerializeField] private bool startVisible = true;

    private Vector2 shownPosition;
    private Vector2 hiddenPosition;
    private bool visible;
    private float progress;

    private void Awake()
    {
        if (panel == null)
        {
            panel = (RectTransform)transform;
        }

        Vector2 direction = slideDirection.sqrMagnitude > 0f ? slideDirection.normalized : Vector2.left;
        float distance = Mathf.Abs(direction.x) * panel.rect.width + Mathf.Abs(direction.y) * panel.rect.height + slideMargin;

        shownPosition = panel.anchoredPosition;
        hiddenPosition = shownPosition + direction * distance;
        visible = startVisible;
        progress = visible ? 1f : 0f;
        Apply();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }

        float target = visible ? 1f : 0f;

        if (Mathf.Approximately(progress, target))
        {
            return;
        }

        progress = Mathf.MoveTowards(progress, target, Time.unscaledDeltaTime / Mathf.Max(slideSeconds, 0.01f));
        Apply();
    }

    private void Apply()
    {
        float eased = Mathf.SmoothStep(0f, 1f, progress);
        panel.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, eased);
    }
}
