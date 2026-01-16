using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Header("Shake - Normal Mode (Default)")]
    [SerializeField] private float normalMissShake = 0.05f;
    [SerializeField] private float normalHitShake = 0.15f;
    [SerializeField] private float normalHurtShake = 0.25f;
    [SerializeField] private float normalShakeDuration = 0.1f;
    
    [Header("Shake - NoShake Mode (Atomik)")]
    [SerializeField] private float noShakeMissShake = 0.001f;
    [SerializeField] private float noShakeHitShake = 0.002f;
    [SerializeField] private float noShakeHurtShake = 0.003f;
    [SerializeField] private float noShakeDuration = 0.01f;
    
    [Header("Shake Settings")]
    [SerializeField] private ShakeMode currentShakeMode = ShakeMode.Normal;
    
    [Header("Pixel Perfect Settings")]
    [SerializeField] private int pixelsPerUnit = 16;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;
    
    private float activeMissShake;
    private float activeHitShake;
    private float activeHurtShake;
    private float activeShakeDuration;
    
    private Vector3 shakeOffset;
    private Vector3 originalPosition;
    private Coroutine shakeRoutine;
    private float unitsPerPixel;

    public enum ShakeMode
    {
        Normal,
        NoShake
    }

    private void Awake()
    {
        originalPosition = transform.localPosition;
        unitsPerPixel = 1f / pixelsPerUnit;
        
        LoadShakeMode();
        ApplyShakeMode();
        
        if (showDebugLogs)
        {
                                }
    }

    private void LateUpdate()
    {
        // Parent pozisyonunu orijinal + shake offset yap
        transform.localPosition = originalPosition + shakeOffset;
    }

    // ================= SHAKE MODE SYSTEM =================
    
    public void SetShakeMode(ShakeMode mode)
    {
        currentShakeMode = mode;
        ApplyShakeMode();
        SaveShakeMode();
        
        if (showDebugLogs)
        {
                    }
    }
    
    public ShakeMode GetShakeMode()
    {
        return currentShakeMode;
    }
    
    private void ApplyShakeMode()
    {
        switch (currentShakeMode)
        {
            case ShakeMode.Normal:
                activeMissShake = normalMissShake;
                activeHitShake = normalHitShake;
                activeHurtShake = normalHurtShake;
                activeShakeDuration = normalShakeDuration;
                
                if (showDebugLogs)
                {
                                                        }
                break;
                
            case ShakeMode.NoShake:
                activeMissShake = noShakeMissShake;
                activeHitShake = noShakeHitShake;
                activeHurtShake = noShakeHurtShake;
                activeShakeDuration = noShakeDuration;
                
                if (showDebugLogs)
                {
                                                        }
                break;
        }
    }
    
    private void SaveShakeMode()
    {
        PlayerPrefs.SetInt("CameraShakeMode", (int)currentShakeMode);
        PlayerPrefs.Save();
    }
    
    private void LoadShakeMode()
    {
        currentShakeMode = (ShakeMode)PlayerPrefs.GetInt("CameraShakeMode", (int)ShakeMode.Normal);
    }

    // ================= PUBLIC API =================
    
    public void TriggerMissShake()
    {
        if (showDebugLogs)
        {
                    }
        TriggerShake(activeMissShake);
    }
    
    public void TriggerHitShake(int comboMultiplier = 1)
    {
        float finalIntensity = activeHitShake * comboMultiplier;
        if (showDebugLogs)
        {
                    }
        TriggerShake(finalIntensity);
    }
    
    public void TriggerHurtShake(bool isProjectile = false)
    {
        float intensity = isProjectile ? activeHurtShake * 0.5f : activeHurtShake;
        if (showDebugLogs)
        {
                    }
        TriggerShake(intensity);
    }
    
    private void TriggerShake(float intensity)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(intensity));
    }
    
    private IEnumerator ShakeRoutine(float intensity)
    {
        float elapsed = 0f;
        int frameCount = 0;
        
        if (showDebugLogs)
        {
                    }
        
        while (elapsed < activeShakeDuration)
        {
            // Pixel-aligned shake
            int xPixels = Random.Range(-Mathf.RoundToInt(intensity * pixelsPerUnit), 
                                        Mathf.RoundToInt(intensity * pixelsPerUnit));
            int yPixels = Random.Range(-Mathf.RoundToInt(intensity * pixelsPerUnit), 
                                        Mathf.RoundToInt(intensity * pixelsPerUnit));
            
            shakeOffset = new Vector3(xPixels * unitsPerPixel, yPixels * unitsPerPixel, 0);
            
            // İlk frame'de offset'i logla
            if (frameCount == 0 && showDebugLogs)
            {
                            }
            
            elapsed += Time.deltaTime;
            frameCount++;
            yield return null;
        }
        
        shakeOffset = Vector3.zero;
        
        if (showDebugLogs)
        {
                    }
    }
    
    // ================= DEBUG =================
    
    [ContextMenu("Test Shake - Normal Mode")]
    private void TestNormalMode()
    {
                SetShakeMode(ShakeMode.Normal);
        TriggerHurtShake();
    }
    
    [ContextMenu("Test Shake - NoShake Mode")]
    private void TestNoShakeMode()
    {
                SetShakeMode(ShakeMode.NoShake);
        TriggerHurtShake();
    }
    
    [ContextMenu("Debug - Print Current Values")]
    private void DebugPrintCurrentValues()
    {
                                                                            }
    
    [ContextMenu("Debug - Test Both Modes Back-to-Back")]
    private void TestBothModes()
    {
        StartCoroutine(TestBothModesRoutine());
    }
    
    private IEnumerator TestBothModesRoutine()
    {
                // Normal mode test
                yield return new WaitForSeconds(1f);
        SetShakeMode(ShakeMode.Normal);
        TriggerHurtShake();
        
        yield return new WaitForSeconds(2f);
        
        // NoShake mode test
                yield return new WaitForSeconds(1f);
        SetShakeMode(ShakeMode.NoShake);
        TriggerHurtShake();
        
            }
}