using UnityEngine;
using UnityEngine.UI;

public class PauseBlurBackground : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RawImage blurImage;
    
    [Header("Blur Settings")]
    [SerializeField] private int downscaleFactor = 4;
    
    private RenderTexture blurredTexture;
    private Camera mainCam;
    
    private void Awake()
    {
        mainCam = Camera.main;
        
        if (blurImage != null)
            blurImage.enabled = false;
    }
    
    public void CaptureAndBlur()
    {
        if (mainCam == null)
            mainCam = Camera.main;
        
        int width = Screen.width / downscaleFactor;
        int height = Screen.height / downscaleFactor;
        
        // Eski texture'ı temizle
        if (blurredTexture != null)
            blurredTexture.Release();
        
        // Yeni RenderTexture
        blurredTexture = new RenderTexture(width, height, 0);
        
        // Kameranın şu anki target'ını kaydet
        RenderTexture originalTarget = mainCam.targetTexture;
        
        // Kamerayı bizim texture'a renderla
        mainCam.targetTexture = blurredTexture;
        mainCam.Render();
        mainCam.targetTexture = originalTarget;
        
        // UI'a ata
        if (blurImage != null)
        {
            blurImage.texture = blurredTexture;
            blurImage.enabled = true;
        }
        
        Debug.Log("[Blur] Capture complete!");
    }
    
    public void ClearBlur()
    {
        if (blurImage != null)
            blurImage.enabled = false;
    }
    
    private void OnDestroy()
    {
        if (blurredTexture != null)
        {
            blurredTexture.Release();
            blurredTexture = null;
        }
    }
}