using UnityEngine;

public class SpawnPoints : MonoBehaviour
{
    [SerializeField] private Transform[] points;

    public Vector3 GetPosition(int playerIndex)
    {
        return GetPoint(playerIndex).position;
    }

    public Quaternion GetRotation(int playerIndex)
    {
        return GetPoint(playerIndex).rotation;
    }

    private Transform GetPoint(int playerIndex)
    {
        int pointIndex = playerIndex % points.Length;
        return points[pointIndex];
    }
}
