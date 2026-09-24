using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [SerializeField] private float distance = 5f;
    [SerializeField] private float pivotHeight = 1.6f;
    [SerializeField] private float pitch = 15f;
    [SerializeField] private float positionFollowSpeed = 15f;
    [SerializeField] private float rotationFollowSpeed = 8f;

    private float yaw;
    private Vector3 smoothedPivot;
    private bool hasTarget;

    private void LateUpdate()
    {
        if (LocalAvatar.Target == null)
        {
            hasTarget = false;
            return;
        }

        Transform target = LocalAvatar.Target;
        Vector3 pivot = target.position + Vector3.up * pivotHeight;

        if (hasTarget == false)
        {
            smoothedPivot = pivot;
            yaw = target.eulerAngles.y;
            hasTarget = true;
        }

        float positionBlend = 1f - Mathf.Exp(-positionFollowSpeed * Time.deltaTime);
        float rotationBlend = 1f - Mathf.Exp(-rotationFollowSpeed * Time.deltaTime);

        smoothedPivot = Vector3.Lerp(smoothedPivot, pivot, positionBlend);
        yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y, rotationBlend);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        transform.rotation = rotation;
        transform.position = smoothedPivot - rotation * Vector3.forward * distance;
    }
}
