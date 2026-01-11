using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Death Sequence")]
    [SerializeField] private float deathRotationDuration = 0.5f;
    [Tooltip("Ali'nin -90° dönme süresi")]
    [SerializeField] private AnimationCurve deathRotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Tooltip("Ease-in-out curve")]
    [SerializeField] private AudioClip deathSfx;
    [Tooltip("Ölüm anında çalacak ses (örn: 'ugh', düşme sesi)")]
    [SerializeField] private float deathSequenceDelay = 0.5f;
    [Tooltip("Ölüm animasyonundan sonra bekleme süresi")]
    
    [Header("Red Screen Effect")]
    [SerializeField] private Image redScreenOverlay;
    [Tooltip("Tam ekran kırmızı Image (CanvasGroup içinde olmalı)")]
    [SerializeField] private float redScreenFadeInDuration = 0.25f;
    [SerializeField] private float redScreenFadeOutDuration = 0.25f;
    [SerializeField] private Color redScreenColor = new Color(1f, 0f, 0f, 0.6f);
    [Tooltip("Kırmızı ekran rengi (alpha ile transparanlık)")]
    
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    
    [Header("State Control")]
    [Tooltip("Bu state'lerde Game Over gösterilebilir")]
    [SerializeField] private int[] allowedStates = { 2, 3, 4, 5, 6, 7, 8, 9, 10 }; // Combat state'ler
    
    private static GameOverManager instance;
    
    public static GameOverManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameOverManager>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        // Singleton pattern
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Panel başlangıçta kapalı
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        // Red screen overlay başlangıçta şeffaf
        if (redScreenOverlay != null)
        {
            redScreenOverlay.gameObject.SetActive(true);
            Color col = redScreenOverlay.color;
            col.a = 0f;
            redScreenOverlay.color = col;
        }
        else
        {
            Debug.LogWarning("[GameOver] ⚠️ Red Screen Overlay is not assigned!");
        }
        
        // Button listener'lar
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetry);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }
        
        Debug.Log("[GameOver] Manager initialized");
    }
    
    /// <summary>
    /// Game Over ekranını göster (PlayerController'dan çağrılır)
    /// </summary>
    public void ShowGameOver()
    {
        // Mevcut state'i kontrol et
        int currentState = PlayerPrefs.GetInt("GameState", 1);
        
        // State izin verilen listede mi?
        bool isAllowedState = System.Array.Exists(allowedStates, state => state == currentState);
        
        if (!isAllowedState)
        {
            Debug.LogWarning($"[GameOver] State {currentState} is not allowed for Game Over! Skipping...");
            
            // Tutorial/cutscene state'lerinde direkt retry
            RetryCurrentState();
            return;
        }
        
        Debug.Log($"[GameOver] ☠️ Death sequence starting! Current state: {currentState}");
        
        // DEATH SEQUENCE başlat (coroutine)
        StartCoroutine(DeathSequence());
    }
    
    /// <summary>
    /// Ölüm sekansı: Animasyon → Düşme → Kırmızı ekran → Game Over
    /// </summary>
    private System.Collections.IEnumerator DeathSequence()
    {
        // Player'ı bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[GameOver] ❌ Player not found! Skipping death sequence.");
            ShowGameOverPanel();
            yield break;
        }
        
        Animator playerAnimator = playerObj.GetComponent<Animator>();
        Transform playerTransform = playerObj.transform;
        
        Debug.Log("[GameOver] 💀 Phase 1: Death animation & rotation");
        
        // PHASE 1: isDead trigger + Rotasyon
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("isDead");
            Debug.Log("[GameOver] 🎬 isDead trigger sent");
        }
        
        // Death SFX çal
        if (audioSource != null && deathSfx != null)
        {
            audioSource.PlayOneShot(deathSfx);
            Debug.Log("[GameOver] 🔊 Death SFX playing");
        }
        
        // Ali'yi -90° döndür (yere düşme)
        float elapsed = 0f;
        Vector3 startRotation = playerTransform.eulerAngles;
        float startZ = startRotation.z;
        float targetZ = startZ - 90f; // Sadece Z ekseninde -90°
        
        while (elapsed < deathRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathRotationDuration;
            float curveT = deathRotationCurve.Evaluate(t);
            
            // Sadece Z eksenini değiştir
            float newZ = Mathf.Lerp(startZ, targetZ, curveT);
            playerTransform.eulerAngles = new Vector3(startRotation.x, startRotation.y, newZ);
            
            yield return null;
        }
        
        // Final rotation
        playerTransform.eulerAngles = new Vector3(startRotation.x, startRotation.y, targetZ);
        Debug.Log($"[GameOver] ⚰️ Player rotated from Z={startZ:F1}° to Z={targetZ:F1}°");
        
        // PHASE 2: Death sequence delay
        yield return new WaitForSeconds(deathSequenceDelay);
        
        // PHASE 3: Kırmızı ekran fade-in
        Debug.Log("[GameOver] 🔴 Phase 2: Red screen fade-in");
        yield return StartCoroutine(RedScreenFadeIn());
        
        // PHASE 4: Kırmızı ekran fade-out
        Debug.Log("[GameOver] ⚪ Phase 3: Red screen fade-out");
        yield return StartCoroutine(RedScreenFadeOut());
        
        // PHASE 5: Game Over panel göster
        Debug.Log("[GameOver] 💀 Phase 4: Showing Game Over panel");
        ShowGameOverPanel();
    }
    
    private System.Collections.IEnumerator RedScreenFadeIn()
    {
        if (redScreenOverlay == null)
        {
            Debug.LogWarning("[GameOver] ⚠️ Red screen overlay is null!");
            yield break;
        }
        
        float elapsed = 0f;
        Color startColor = redScreenOverlay.color;
        Color targetColor = redScreenColor;
        
        while (elapsed < redScreenFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redScreenFadeInDuration;
            
            redScreenOverlay.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        redScreenOverlay.color = targetColor;
    }
    
    private System.Collections.IEnumerator RedScreenFadeOut()
    {
        if (redScreenOverlay == null)
        {
            yield break;
        }
        
        float elapsed = 0f;
        Color startColor = redScreenOverlay.color;
        Color targetColor = redScreenColor;
        targetColor.a = 0f;
        
        while (elapsed < redScreenFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redScreenFadeOutDuration;
            
            redScreenOverlay.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        redScreenOverlay.color = targetColor;
    }
    
    private void ShowGameOverPanel()
    {
        // Time'ı durdur
        Time.timeScale = 0f;
        
        // Panel'i aç
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // Game Over SFX çal
        PlayGameOverSfx();
        
        // Input'u devre dışı bırak
        DisablePlayerInput();
    }
    
    /// <summary>
    /// Retry - Mevcut state'in başından devam et
    /// </summary>
    public void OnRetry()
    {
        Debug.Log("[GameOver] 🔄 Retry clicked - Reloading current state...");
        
        PlayButtonClickSfx();
        
        RetryCurrentState();
    }
    
    /// <summary>
    /// Main Menu - Ana menüye dön (state korunur)
    /// </summary>
    public void OnMainMenu()
    {
        Debug.Log("[GameOver] 🏠 Main Menu clicked - Returning to menu...");
        
        PlayButtonClickSfx();
        
        // Time'ı normale döndür
        Time.timeScale = 1f;
        
        // Ana menüye dön (state PlayerPrefs'te zaten kayıtlı)
        SceneManager.LoadScene("MainMenu");
    }
    
    private void RetryCurrentState()
    {
        // Time'ı normale döndür
        Time.timeScale = 1f;
        
        // Mevcut state PlayerPrefs'te zaten kayıtlı
        // WorldMap scene'i reload et → CutsceneChief otomatik state'i yükler
        SceneManager.LoadScene("WorldMap");
    }
    
    private void PlayGameOverSfx()
    {
        if (audioSource != null && gameOverSfx != null)
        {
            // Time.timeScale = 0 olduğu için unscaled audio kullan
            audioSource.PlayOneShot(gameOverSfx);
            Debug.Log("[GameOver] 🔊 Playing Game Over SFX");
        }
    }
    
    private void PlayButtonClickSfx()
    {
        if (audioSource != null && buttonClickSfx != null)
        {
            audioSource.PlayOneShot(buttonClickSfx);
        }
    }
    
    private void DisablePlayerInput()
    {
        // Player input'u kapat (optional)
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
            Debug.Log("[GameOver] Player input disabled");
        }
    }
    
    private void OnDestroy()
    {
        // Button listener'ları temizle
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetry);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }
    }
}