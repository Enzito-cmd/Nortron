using UnityEngine;

public class ColorSelector : MonoBehaviour
{
    [SerializeField] private Renderer[] previewRenderers;
    [SerializeField] private Transform spinner;
    [SerializeField] private float spinSpeed = 45f;

    private int shownIndex = -1;

    public void Step(int direction)
    {
        PlayerIdentity identity = PlayerIdentity.Local;

        if (identity == null)
        {
            return;
        }

        int next = (identity.ColorIndex + direction + PlayerIdentity.ColorCount) % PlayerIdentity.ColorCount;
        identity.RequestColor(next);
    }

    private void Update()
    {
        if (spinner != null)
        {
            spinner.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        }

        PlayerIdentity identity = PlayerIdentity.Local;

        if (identity == null || identity.ColorIndex == shownIndex)
        {
            return;
        }

        shownIndex = identity.ColorIndex;
        identity.TintRenderers(previewRenderers, shownIndex);
    }
}
