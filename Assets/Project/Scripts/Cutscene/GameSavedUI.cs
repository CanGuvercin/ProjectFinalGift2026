using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameSavedUI : MonoBehaviour
{
    //
    [Header("UI References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [Tooltip("Canvas Group component (for fade effect)")]
    
    [Header("Timing")]
    [SerializeField] private float delayBeforeShow = 1f;
    [Tooltip("State geçişinden sonra kaç saniye bekle")]
    [SerializeField] private float displayDuration = 2f;
    [Tooltip("UI kaç saniye görünür kalsın")]
    [SerializeField] private float fadeOutDuration = 0.5f;
    [Tooltip("Fade out animasyon süresi")]
    
    [Header("Animation")]
    [SerializeField] private AnimationCurve fadeOutCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);
    [Tooltip("Fade out easing curve")]
    
    private Coroutine currentAnimation;
    
    private void Awake()
    {
        // Canvas Group yoksa ekle
        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }
        
        // Başlangıçta görünmez yap
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
    
    /// <summary>
    /// "Game Saved" UI'ını göster (CutsceneChief'ten çağrılır)
    /// </summary>
    public void ShowGameSaved()
    {
        // Önceki animasyon varsa durdur
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        currentAnimation = StartCoroutine(GameSavedSequence());
    }
    
    private IEnumerator GameSavedSequence()
    {
        // 1. Delay before show
        yield return new WaitForSeconds(delayBeforeShow);
        
        // 2. Instant show (alpha = 1)
        gameObject.SetActive(true);
        canvasGroup.alpha = 1f;
        
        Debug.Log("[GameSavedUI] 💾 Game Saved UI visible");
        
        // 3. Display duration
        yield return new WaitForSeconds(displayDuration);
        
        // 4. Fade out
        float elapsed = 0f;
        
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float curveT = fadeOutCurve.Evaluate(t);
            
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, curveT);
            
            yield return null;
        }
        
        // 5. Hide completely
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
        
        Debug.Log("[GameSavedUI] 💾 Game Saved UI hidden");
        
        currentAnimation = null;
    }
    
    /// <summary>
    /// Hemen gizle (scene değişikliği öncesi)
    /// </summary>
    public void HideImmediately()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        canvasGroup.alpha = 0f;
        gameObject.SetActive(false);
    }
}