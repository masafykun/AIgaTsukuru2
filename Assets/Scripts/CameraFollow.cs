using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float distance  = 9f;
    public float height    = 5f;
    public float smoothing = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        var desired = target.position
                      - target.forward * distance
                      + Vector3.up * height;

        transform.position = Vector3.Lerp(transform.position, desired, smoothing * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 0.5f);
    }
}
