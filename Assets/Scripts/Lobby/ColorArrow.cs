using UnityEngine;

public class ColorArrow : MonoBehaviour
{
    private const float HoverSmoothing = 10f;

    [SerializeField] private ColorSelector selector;
    [SerializeField] private int direction = 1;
    [SerializeField] private float bobHeight = 0.1f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float hoverScale = 1.15f;

    private Vector3 basePosition;
    private Vector3 baseScale;
    private bool hovered;

    private void Awake()
    {
        basePosition = transform.localPosition;
        baseScale = transform.localScale;
    }

    private void Update()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        Vector3 targetScale = hovered ? baseScale * hoverScale : baseScale;

        transform.localPosition = basePosition + Vector3.up * bob;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, HoverSmoothing * Time.deltaTime);
    }

    private void OnMouseEnter()
    {
        hovered = true;
    }

    private void OnMouseExit()
    {
        hovered = false;
    }

    private void OnMouseDown()
    {
        selector.Step(direction);
    }
}
