using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MenuBackgroundAnimator : MonoBehaviour
{
    [Header("Background Image")]
    [SerializeField] private RectTransform backgroundImage;
    
    [Header("Movement Settings")]
    [SerializeField] private float movementRange = 30f; // Merkez noktasından max sapma (piksel)
    [SerializeField] private float moveDuration = 4f; // Her hareket süresi (saniye)
    [SerializeField] private float minMoveDelay = 2f; // Hareket arası min bekleme
    [SerializeField] private float maxMoveDelay = 4f; // Hareket arası max bekleme
    //too much for a parrallax effect
    
    
    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 1.0f; // Min scale
    [SerializeField] private float maxZoom = 1.15f; // Max scale
    
    [Header("Fade Settings")]
    [SerializeField] private CanvasGroup fadeOverlay; // Siyah overlay (fade için)
    [SerializeField] private float fadeDuration = 0.8f; // Fade süresi
    [SerializeField] private bool enableFade = true; // Fade açık/kapalı
    
    private Vector2 originalPosition;
    private Vector3 originalScale;
    
    private void Start()
    {
        if (backgroundImage == null)
        {
            Debug.LogError("[MenuBG] Background Image reference missing!");
            return;
        }
        
        // Orijinal değerleri kaydet
        originalPosition = backgroundImage.anchoredPosition;
        originalScale = backgroundImage.localScale;
        
        // Fade overlay varsa başlangıçta görünmez yap
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
            fadeOverlay.blocksRaycasts = false;
        }
        
        Debug.Log($"[MenuBG] Started! Original pos: {originalPosition}, scale: {originalScale}");
        
        // Animasyon döngüsünü başlat
        StartCoroutine(AnimationLoop());
    }
    
    private IEnumerator AnimationLoop()
    {
        while (true)
        {
            // 1. Random hedef pozisyon (merkeze yakın)
            Vector2 targetPos = originalPosition + new Vector2(
                Random.Range(-movementRange, movementRange),
                Random.Range(-movementRange, movementRange)
            );
            
            // 2. Random zoom değeri
            float targetZoom = Random.Range(minZoom, maxZoom);
            Vector3 targetScale = originalScale * targetZoom;
            
            Debug.Log($"[MenuBG] New target → Pos: {targetPos}, Zoom: {targetZoom:F2}");
            
            // 3. Fade to black (opsiyonel)
            if (enableFade && fadeOverlay != null)
            {
                yield return StartCoroutine(FadeToBlack());
            }
            
            // 4. Smooth hareket + zoom
            float elapsed = 0f;
            Vector2 startPos = backgroundImage.anchoredPosition;
            Vector3 startScale = backgroundImage.localScale;
            
            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / moveDuration;
                
                // Smooth ease in-out
                float smoothT = Mathf.SmoothStep(0f, 1f, t);
                
                // Pozisyon lerp
                backgroundImage.anchoredPosition = Vector2.Lerp(startPos, targetPos, smoothT);
                
                // Scale lerp
                backgroundImage.localScale = Vector3.Lerp(startScale, targetScale, smoothT);
                
                yield return null;
            }
            
            // Final pozisyonu garantile
            backgroundImage.anchoredPosition = targetPos;
            backgroundImage.localScale = targetScale;
            
            // 5. Fade from black (opsiyonel)
            if (enableFade && fadeOverlay != null)
            {
                yield return StartCoroutine(FadeFromBlack());
            }
            
            // 6. Random bekleme
            float waitTime = Random.Range(minMoveDelay, maxMoveDelay);
            Debug.Log($"[MenuBG] Waiting {waitTime:F1}s...");
            yield return new WaitForSeconds(waitTime);
        }
    }
    
    private IEnumerator FadeToBlack()
    {
        if (fadeOverlay == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            yield return null;
        }
        
        fadeOverlay.alpha = 1f;
    }
    
    private IEnumerator FadeFromBlack()
    {
        if (fadeOverlay == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            yield return null;
        }
        
        fadeOverlay.alpha = 0f;
    }
    
    private void OnDisable()
    {
        // Animasyon durdur
        StopAllCoroutines();
        
        // Orijinal değerlere dön
        if (backgroundImage != null)
        {
            backgroundImage.anchoredPosition = originalPosition;
            backgroundImage.localScale = originalScale;
        }
        
        if (fadeOverlay != null)
        {
            fadeOverlay.alpha = 0f;
        }
    }
}