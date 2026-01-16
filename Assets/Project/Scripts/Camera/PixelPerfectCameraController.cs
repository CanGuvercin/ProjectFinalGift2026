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
    [SerializeField] private int referenceResolutionY = 180;
    
    [Header("Zoom")]
    [SerializeField] private float baseZoom = 1f;
    [SerializeField] private float zoomSpeed = 8f;
    [SerializeField] private float attackZoomOut = 1.05f;
    [SerializeField] private float hitZoomStep = 0.05f;
    [SerializeField] private int maxHitCombo = 3;
    
    [Header("Shake Reference (Parent)")]
    [SerializeField] private CameraShake cameraShake;
    
    private Camera cam;
    private float targetZoom = 1f;
    private int currentCombo = 0;
    
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
        
        // Parent'tan CameraShake bul
        if (cameraShake == null)
        {
            cameraShake = GetComponentInParent<CameraShake>();
        }
        
        CalculatePixelPerfectSize();
        targetZoom = baseZoom;
    }
    
    private void CalculatePixelPerfectSize()
    {
        screenHeight = Screen.height;
        unitsPerPixel = 1f / pixelsPerUnit;
        baseOrthographicSize = (referenceResolutionY / 2f) * unitsPerPixel;
        cam.orthographicSize = baseOrthographicSize * baseZoom;
        
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
        
        // 4. Pozisyonu ayarla (shake parent'ta yapılıyor)
        transform.position = snappedPos;
    }
    
    private Vector3 SnapToPixelGrid(Vector3 position)
    {
        float pixelSize = unitsPerPixel;
        float snappedX = Mathf.Round(position.x / pixelSize) * pixelSize;
        float snappedY = Mathf.Round(position.y / pixelSize) * pixelSize;
        return new Vector3(snappedX, snappedY, position.z);
    }

    // ================= PUBLIC API =================
    
    public void OnAttackMiss()
    {
        targetZoom = baseZoom * attackZoomOut;
        if (cameraShake != null) cameraShake.TriggerMissShake();
        StartCoroutine(ResetZoom(0.15f));
    }
    
    public void OnAttackHit()
    {
        currentCombo = Mathf.Min(currentCombo + 1, maxHitCombo);
        
        float zoomMultiplier = 1f - (hitZoomStep * currentCombo);
        targetZoom = baseZoom * zoomMultiplier;
        
        if (cameraShake != null) cameraShake.TriggerHitShake(currentCombo);
        StartCoroutine(ResetZoom(0.5f));
    }
    
    public void OnPlayerHurt(float damage = 10f, bool isProjectile = false)
    {
        if (cameraShake != null) cameraShake.TriggerHurtShake(isProjectile);
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
    
    private void OnPreCull()
    {
        if (screenHeight != Screen.height)
        {
            CalculatePixelPerfectSize();
        }
    }
}