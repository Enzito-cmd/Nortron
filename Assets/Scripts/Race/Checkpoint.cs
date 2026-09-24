using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private BoxCollider area;

    public bool Contains(Vector3 worldPosition)
    {
        Vector3 localPosition = area.transform.InverseTransformPoint(worldPosition) - area.center;
        Vector3 halfSize = area.size * 0.5f;

        return Mathf.Abs(localPosition.x) <= halfSize.x
            && Mathf.Abs(localPosition.y) <= halfSize.y
            && Mathf.Abs(localPosition.z) <= halfSize.z;
    }
}
