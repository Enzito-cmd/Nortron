using UnityEngine;

public class BoostPickup : MonoBehaviour
{
    private const string IconResource = "BoostIcon";

    [SerializeField] private float pickupRadius = 2f;
    [SerializeField] private float respawnSeconds = 10f;
    [SerializeField] private GameObject visual;
    [SerializeField] private float iconSize = 1.4f;
    [SerializeField] private float hoverHeight = 1.2f;
    [SerializeField] private float bobHeight = 0.15f;
    [SerializeField] private float bobSpeed = 2.5f;

    private bool faceCamera;
    private Vector3 visualBasePosition;

    public float RespawnSeconds => respawnSeconds;

    public bool IsNear(Vector3 position)
    {
        return (position - transform.position).sqrMagnitude <= pickupRadius * pickupRadius;
    }

    public void SetAvailable(bool available)
    {
        if (visual != null && visual.activeSelf != available)
        {
            visual.SetActive(available);
        }
    }

    private void Awake()
    {
        if (visual == null)
        {
            visual = CreateIcon();
            faceCamera = true;
        }

        visualBasePosition = visual.transform.localPosition;
    }

    private void LateUpdate()
    {
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobHeight;
        visual.transform.localPosition = visualBasePosition + Vector3.up * bob;

        if (faceCamera && Camera.main != null)
        {
            visual.transform.rotation = Camera.main.transform.rotation;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
        Gizmos.DrawSphere(transform.position + Vector3.up * hoverHeight, 0.2f);
    }

    private GameObject CreateIcon()
    {
        GameObject icon = new GameObject("BoostIcon");
        icon.transform.SetParent(transform, false);
        icon.transform.localPosition = Vector3.up * hoverHeight;

        Texture2D texture = Resources.Load<Texture2D>(IconResource);

        if (texture != null)
        {
            Rect area = new Rect(0f, 0f, texture.width, texture.height);
            Sprite sprite = Sprite.Create(texture, area, new Vector2(0.5f, 0.5f), texture.width / iconSize, 0, SpriteMeshType.FullRect);
            icon.AddComponent<SpriteRenderer>().sprite = sprite;
        }

        return icon;
    }
}
