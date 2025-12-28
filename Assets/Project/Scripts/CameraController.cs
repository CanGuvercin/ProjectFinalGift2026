using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target; // Player

    [Header("Follow Settings")]
    [SerializeField] private float followSpeed = 5f;
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10);
    
    [Header("Zoom Settings")]
    [SerializeField] private float baseOrthographicSize = 5f;
    [SerializeField] private float zoomSpeed = 8f;
    
    [Header("Attack Zoom (Miss)")]
    [SerializeField] private float attackZoomOut = 1.05f; // Zoom out miktarı
    [SerializeField] private float attackZoomDuration = 0.15f;
    
    [Header("Hit Zoom (Multi-hit Combo)")]
    [SerializeField] private float hitZoomStep = 0.05f; // Her hit'te zoom miktarı (0.95, 0.90, 0.85...)
    [SerializeField] private int maxHitZoomSteps = 3; // Max 3 hit combo zoom
    [SerializeField] private float hitZoomResetDelay = 0.5f; // Hit yoksa zoom sıfırlanır
    
    [Header("Screen Shake")]
    [SerializeField] private float shakeOnHitIntensity = 0.15f;
    [SerializeField] private float shakeOnMissIntensity = 0.05f;
    [SerializeField] private float shakeDuration = 0.1f;
    
    private Camera cam;
    private float targetZoom;
    private int currentHitCombo = 0;
    private Coroutine zoomResetRoutine;
    private Coroutine shakeRoutine;
    private Vector3 shakeOffset;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        
        if (cam == null)
        {
            Debug.LogError("Camera component not found!");
            return;
        }

        // Başlangıç zoom
        cam.orthographicSize = baseOrthographicSize;
        targetZoom = baseOrthographicSize;
        
        // Player'ı otomatik bul
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
                target = player.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Target'ı takip et
        FollowTarget();
        
        // Zoom smooth geçişi
        SmoothZoom();
    }

    private void FollowTarget()
    {
        Vector3 desiredPosition = target.position + offset + shakeOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    }

    private void SmoothZoom()
    {
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }

    // ================= PUBLIC API =================

    /// <summary>
    /// Kılıç sallandı ama hedef yok (miss) - Hafif zoom out
    /// </summary>
    public void OnAttackMiss()
    {
        // Zoom out yap
        targetZoom = baseOrthographicSize * attackZoomOut;
        
        // Hafif shake
        TriggerScreenShake(shakeOnMissIntensity);
        
        // Hızlıca geri dön
        if (zoomResetRoutine != null)
            StopCoroutine(zoomResetRoutine);
        
        zoomResetRoutine = StartCoroutine(ResetZoomAfterDelay(attackZoomDuration));
        
        Debug.Log("[CAMERA] Attack Miss - Zoom Out");
    }

    /// <summary>
    /// Düşmana hit! - Combo zoom in (her hit'te daha fazla)
    /// </summary>
    public void OnAttackHit()
    {
        // Combo sayacını artır (max limit var)
        currentHitCombo = Mathf.Min(currentHitCombo + 1, maxHitZoomSteps);
        
        // Zoom in yap (her hit daha fazla zoom)
        float zoomMultiplier = 1f - (hitZoomStep * currentHitCombo);
        targetZoom = baseOrthographicSize * zoomMultiplier;
        
        // Güçlü shake
        TriggerScreenShake(shakeOnHitIntensity * currentHitCombo);
        
        // Combo reset timer'ı yeniden başlat
        if (zoomResetRoutine != null)
            StopCoroutine(zoomResetRoutine);
        
        zoomResetRoutine = StartCoroutine(ResetZoomAfterDelay(hitZoomResetDelay));
        
        Debug.Log($"[CAMERA] Hit! Combo: {currentHitCombo}, Zoom: {zoomMultiplier:F2}x");
    }

    /// <summary>
    /// Manuel zoom reset (örn: dash sonrası)
    /// </summary>
    public void ResetZoom()
    {
        targetZoom = baseOrthographicSize;
        currentHitCombo = 0;
    }

    // ================= ZOOM RESET =================

    private IEnumerator ResetZoomAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Zoom'u base'e geri getir
        targetZoom = baseOrthographicSize;
        currentHitCombo = 0;
        
        Debug.Log("[CAMERA] Zoom Reset");
    }

    // ================= SCREEN SHAKE =================

    private void TriggerScreenShake(float intensity)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);
        
        shakeRoutine = StartCoroutine(ScreenShakeRoutine(intensity));
    }

    private IEnumerator ScreenShakeRoutine(float intensity)
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            // Random shake offset
            float x = Random.Range(-1f, 1f) * intensity;
            float y = Random.Range(-1f, 1f) * intensity;
            
            shakeOffset = new Vector3(x, y, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Shake bitince sıfırla
        shakeOffset = Vector3.zero;
    }

    // ================= DEBUG =================

    private void OnDrawGizmosSelected()
    {
        if (target != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawWireSphere(target.position, 0.5f);
        }
    }
}