using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    public enum ShakeType { Miss, Hit, PlayerHurtProjectile, PlayerHurtContact }

    [Header("Zoom Settings")]
    [SerializeField] private float baseOrthographicSize = 5f;
    [SerializeField] private float zoomSpeed = 8f;

    [Header("Attack Zoom (Miss)")]
    [SerializeField] private float attackZoomOut = 1.05f;
    [SerializeField] private float attackZoomDuration = 0.15f;

    [Header("Hit Zoom (Multi-hit Combo)")]
    [SerializeField] private float hitZoomStep = 0.05f;
    [SerializeField] private int maxHitZoomSteps = 3;
    [SerializeField] private float hitZoomResetDelay = 0.5f;

    [Header("Screen Shake - Base Intensities")]
    [SerializeField] private float missIntensity = 0.05f;
    [SerializeField] private float hitBaseIntensity = 0.15f;
    [SerializeField] private float hurtProjectileIntensity = 0.12f;
    [SerializeField] private float hurtContactBaseIntensity = 0.25f;

    [Header("Screen Shake - Scaling")]
    [SerializeField] private bool scaleHurtByDamage = true;
    [SerializeField] private float damageToIntensityDivider = 15f;   // damage/15
    [SerializeField] private float hurtMin = 0.25f;
    [SerializeField] private float hurtMax = 0.50f;

    [Header("Screen Shake - Timing")]
    [SerializeField] private float shakeDuration = 0.1f;

    private Camera cam;
    private float targetZoom;
    private int currentHitCombo = 0;

    private Coroutine zoomResetRoutine;
    private Coroutine shakeRoutine;

    private Vector3 originalLocalPos;

    public Vector3 CurrentShakeOffset { get; private set; }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        originalLocalPos = transform.localPosition;

        cam.orthographicSize = baseOrthographicSize;
        targetZoom = baseOrthographicSize;
    }

    private void LateUpdate() => SmoothZoom();

    private void SmoothZoom()
    {
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetZoom, zoomSpeed * Time.deltaTime);
    }

    // ================= PUBLIC API =================

    public void OnAttackMiss()
    {
        targetZoom = baseOrthographicSize * attackZoomOut;
        TriggerShake(ShakeType.Miss);

        RestartZoomReset(attackZoomDuration);
    }

    public void OnAttackHit()
    {
        currentHitCombo = Mathf.Min(currentHitCombo + 1, maxHitZoomSteps);

        float zoomMultiplier = 1f - (hitZoomStep * currentHitCombo);
        targetZoom = baseOrthographicSize * zoomMultiplier;

        // Combo kuvveti: base * combo
        TriggerShake(ShakeType.Hit, comboMultiplier: currentHitCombo);

        RestartZoomReset(hitZoomResetDelay);
    }

    public void ResetZoom()
    {
        targetZoom = baseOrthographicSize;
        currentHitCombo = 0;
    }

    public void OnPlayerHurt(float damageAmount = 10f, bool isProjectile = false)
    {
        if (isProjectile)
        {
            TriggerShake(ShakeType.PlayerHurtProjectile);
            Debug.Log($"[CAMERA] Player Hurt! Type: Projectile");
        }
        else
        {
            // İstersen damage’e göre ölçekle
            float scaled = scaleHurtByDamage
                ? Mathf.Clamp(damageAmount / damageToIntensityDivider, hurtMin, hurtMax)
                : hurtContactBaseIntensity;

            TriggerShake(ShakeType.PlayerHurtContact, overrideIntensity: scaled);
            Debug.Log($"[CAMERA] Player Hurt! Type: Contact, Intensity: {scaled:F2}");
        }
    }

    // ================= INTERNAL =================

    private void RestartZoomReset(float delay)
    {
        if (zoomResetRoutine != null) StopCoroutine(zoomResetRoutine);
        zoomResetRoutine = StartCoroutine(ResetZoomAfterDelay(delay));
    }

    private IEnumerator ResetZoomAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        targetZoom = baseOrthographicSize;
        currentHitCombo = 0;
    }

    private void TriggerShake(ShakeType type, int comboMultiplier = 1, float? overrideIntensity = null)
    {
        float intensity = overrideIntensity ?? GetBaseIntensity(type);

        if (type == ShakeType.Hit)
            intensity *= comboMultiplier;

        StartShake(intensity);
    }

    private float GetBaseIntensity(ShakeType type)
    {
        switch (type)
        {
            case ShakeType.Miss: return missIntensity;
            case ShakeType.Hit: return hitBaseIntensity;
            case ShakeType.PlayerHurtProjectile: return hurtProjectileIntensity;
            case ShakeType.PlayerHurtContact: return hurtContactBaseIntensity;
            default: return 0.1f;
        }
    }

    private void StartShake(float intensity)
    {
        if (shakeRoutine != null) StopCoroutine(shakeRoutine);
        shakeRoutine = StartCoroutine(ScreenShakeRoutine(intensity));
    }

private IEnumerator ScreenShakeRoutine(float intensity)
{
    float elapsed = 0f;
    float pixelsPerUnit = 16f;

    while (elapsed < shakeDuration)
    {
        // Shake miktarını pixel cinsinden hesapla
        int xPixelOffset = Mathf.RoundToInt(Random.Range(-intensity * pixelsPerUnit, intensity * pixelsPerUnit));
        int yPixelOffset = Mathf.RoundToInt(Random.Range(-intensity * pixelsPerUnit, intensity * pixelsPerUnit));
        
        // Pixel offset'i world space'e çevir
        float x = xPixelOffset / pixelsPerUnit;
        float y = yPixelOffset / pixelsPerUnit;

        // Local position'a ekle (zaten snap'li olmalı)
        Vector3 shakePos = originalLocalPos + new Vector3(x, y, 0);
        
        // Ek güvenlik: bir kez daha snap
        shakePos.x = Mathf.Round(shakePos.x * pixelsPerUnit) / pixelsPerUnit;
        shakePos.y = Mathf.Round(shakePos.y * pixelsPerUnit) / pixelsPerUnit;
        
        transform.localPosition = shakePos;

        elapsed += Time.deltaTime;
        yield return null;
    }

    // Bitince tam orijinal pozisyona dön (snap'li)
    Vector3 resetPos = originalLocalPos;
    resetPos.x = Mathf.Round(resetPos.x * pixelsPerUnit) / pixelsPerUnit;
    resetPos.y = Mathf.Round(resetPos.y * pixelsPerUnit) / pixelsPerUnit;
    
    transform.localPosition = resetPos;
}
}
