using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class PixelPerfectCameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    
    [Header("Follow")]
    [SerializeField] private float followSpeed = 3f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Pixel Perfect")]
    [SerializeField] private int pixelsPerUnit = 16;
    [SerializeField] private int referenceResolutionY = 180; // 180p retro feel
    
    [Header("Zoom")]
    [SerializeField] private float baseZoom = 1f;
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float attackZoomOut = 1.05f;
    [SerializeField] private float hitZoomStep = 0.05f;
    [SerializeField] private int maxHitCombo = 3;
    
    [Header("Shake")]
    [SerializeField] private float missShake = 0.05f;
    [SerializeField] private float hitShake = 0.15f;
    [SerializeField] private float hurtShake = 0.25f;
    [SerializeField] private float shakeDuration = 0.1f;
    
    private Camera cam;
    private float targetZoom = 1f;
    private int currentCombo = 0;
    private Vector3 shakeOffset;
    private Coroutine shakeRoutine;
    
    // Pixel perfect hesaplamaları
    private int screenHeight;
    private float unitsPerPixel;
    private float baseOrthographicSize;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
        
        CalculatePixelPerfectSize();
        targetZoom = baseZoom;
    }
    
    private void CalculatePixelPerfectSize()
    {
        screenHeight = Screen.height;
        
        // Pixel perfect orthographic size hesapla
        unitsPerPixel = 1f / pixelsPerUnit;
        baseOrthographicSize = (referenceResolutionY / 2f) * unitsPerPixel;
        
        cam.orthographicSize = baseOrthographicSize * baseZoom;
        
        Debug.Log($"[CAMERA] Pixel Perfect - Size: {cam.orthographicSize:F2}, PPU: {pixelsPerUnit}");
    }

    private void LateUpdate()
    {
        if (target == null) return;
        
        // 1. Zoom hesapla (smooth)
        float desiredSize = baseOrthographicSize * targetZoom;
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, desiredSize, zoomSpeed * Time.deltaTime);
        
        // 2. Follow target
        Vector3 targetPos = target.position + offset;
        Vector3 smoothPos = Vector3.Lerp(transform.position, targetPos, followSpeed * Time.deltaTime);
        
        // 3. Pixel snap position
        Vector3 snappedPos = SnapToPixelGrid(smoothPos);
        
        // 4. Shake ekle (pixel-aligned)
        transform.position = snappedPos + shakeOffset;
    }
    
    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        // Kamera pozisyonunu pixel grid'e hizala
        float pixelSize = unitsPerPixel;
        
        float snappedX = Mathf.Round(position.x / pixelSize) * pixelSize;
        float snappedY = Mathf.Round(position.y / pixelSize) * pixelSize;
        
        return new Vector3(snappedX, snappedY, position.z);
    }

    // ================= PUBLIC API =================
    
    public void OnAttackMiss()
    {
        targetZoom = baseZoom * attackZoomOut;
        TriggerShake(missShake);
        StartCoroutine(ResetZoom(0.15f));
    }
    
    public void OnAttackHit()
    {
        currentCombo = Mathf.Min(currentCombo + 1, maxHitCombo);
        
        float zoomMultiplier = 1f - (hitZoomStep * currentCombo);
        targetZoom = baseZoom * zoomMultiplier;
        
        TriggerShake(hitShake * currentCombo);
        StartCoroutine(ResetZoom(0.5f));
    }
    
    public void OnPlayerHurt(float damage = 10f, bool isProjectile = false)
    {
        float intensity = isProjectile ? hurtShake * 0.5f : hurtShake;
        TriggerShake(intensity);
    }
    
    public void ResetZoom()
    {
        targetZoom = baseZoom;
        currentCombo = 0;
    }
    
    private IEnumerator ResetZoom(float delay)
    {
        yield return new WaitForSeconds(delay);
        targetZoom = baseZoom;
        currentCombo = 0;
    }
    
    private void TriggerShake(float intensity)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ShakeRoutine(intensity));
    }
    
    private IEnumerator ShakeRoutine(float intensity)
    {
        float elapsed = 0f;
        float pixelSize = unitsPerPixel;
        
        while (elapsed < shakeDuration)
        {
            // Pixel-aligned shake
            int xPixels = Random.Range(-Mathf.RoundToInt(intensity * pixelsPerUnit), 
                                        Mathf.RoundToInt(intensity * pixelsPerUnit));
            int yPixels = Random.Range(-Mathf.RoundToInt(intensity * pixelsPerUnit), 
                                        Mathf.RoundToInt(intensity * pixelsPerUnit));
            
            shakeOffset = new Vector3(xPixels * pixelSize, yPixels * pixelSize, 0);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        shakeOffset = Vector3.zero;
    }
    
    // Screen resize handling
    private void OnPreCull()
    {
        if (screenHeight != Screen.height)
        {
            CalculatePixelPerfectSize();
        }
    }
}