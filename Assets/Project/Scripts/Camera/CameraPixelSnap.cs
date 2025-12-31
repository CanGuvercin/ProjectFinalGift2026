using UnityEngine;

[DisallowMultipleComponent]
public class CameraPixelSnap : MonoBehaviour
{
    [Header("Match this with your sprites' Pixels Per Unit (PPU)")]
    [SerializeField] private float pixelsPerUnit = 16f;

    // Use LateUpdate so it runs AFTER player/Cinemachine movement.
    private void LateUpdate()
    {
        Vector3 p = transform.position;

        float snappedX = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
        float snappedY = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;

        transform.position = new Vector3(snappedX, snappedY, p.z);
    }
}
