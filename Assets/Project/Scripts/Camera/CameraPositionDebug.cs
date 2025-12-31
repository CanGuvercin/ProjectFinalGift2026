using UnityEngine;

public class CameraPositionDebug : MonoBehaviour
{
    [SerializeField] private float pixelsPerUnit = 16f;
    
    private void OnGUI()
    {
        Vector3 pos = transform.position;
        
        float subPixelX = (pos.x * pixelsPerUnit) % 1f;
        float subPixelY = (pos.y * pixelsPerUnit) % 1f;
        
        // Normalize to -0.5 to 0.5 range
        if (subPixelX > 0.5f) subPixelX -= 1f;
        if (subPixelY > 0.5f) subPixelY -= 1f;
        
        GUIStyle style = new GUIStyle(GUI.skin.label);
        style.fontSize = 16;
        style.normal.textColor = Color.white;
        
        GUI.Label(new Rect(10, 10, 400, 30), $"Camera X: {pos.x:F4} (offset: {subPixelX:F3})", style);
        GUI.Label(new Rect(10, 40, 400, 30), $"Camera Y: {pos.y:F4} (offset: {subPixelY:F3})", style);
        
        if (Mathf.Abs(subPixelX) > 0.02f || Mathf.Abs(subPixelY) > 0.02f)
        {
            style.normal.textColor = Color.red;
            GUI.Label(new Rect(10, 70, 400, 30), "⚠️ SUB-PIXEL JITTER!", style);
        }
        else
        {
            style.normal.textColor = Color.green;
            GUI.Label(new Rect(10, 70, 400, 30), "✓ Pixel Perfect", style);
        }
    }
}