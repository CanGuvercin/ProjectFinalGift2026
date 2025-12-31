using UnityEngine;

public class CameraPositionDebug : MonoBehaviour
{
    private void OnGUI()
    {
        Vector3 pos = transform.position;
        float ppu = 16f;
        
        float subPixelX = (pos.x * ppu) % 1f;
        float subPixelY = (pos.y * ppu) % 1f;
        
        GUI.Label(new Rect(10, 10, 300, 20), $"Cam X: {pos.x:F4} (sub: {subPixelX:F2})");
        GUI.Label(new Rect(10, 30, 300, 20), $"Cam Y: {pos.y:F4} (sub: {subPixelY:F2})");
        
        if (Mathf.Abs(subPixelX) > 0.01f || Mathf.Abs(subPixelY) > 0.01f)
        {
            GUI.color = Color.red;
            GUI.Label(new Rect(10, 50, 300, 20), "⚠️ SUB-PIXEL OFFSET DETECTED!");
        }
    }
}