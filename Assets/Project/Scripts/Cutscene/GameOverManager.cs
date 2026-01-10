using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
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
        
        Debug.Log($"[GameOver] ☠️ Showing Game Over screen! Current state: {currentState}");
        
        // Time'ı durdur
        Time.timeScale = 0f;
        
        // Panel'i aç
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        // SFX çal
        PlayGameOverSfx();
        
        // Input'u devre dışı bırak (optional)
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