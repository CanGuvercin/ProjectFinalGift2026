using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if !DISABLESTEAMWORKS
using Steamworks;
#endif

public class PauseMenuManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject pauseMenuPanel;
    [SerializeField] private Text pausedTitleText;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button quitButton;
    
    [Header("Settings Manager")]
    [SerializeField] private SettingsManager settingsManager;
    
    [Header("Controller Navigation")]
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;
    
    [Header("Player Input")]
    [SerializeField] private PlayerInput playerInput;
    
    [Header("Pause Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float inputCooldown = 0.2f;
    
    [Header("Blur Effect")]
    [SerializeField] private PauseBlurBackground blurBackground;

    private bool isPaused = false;
    private float lastInputTime = -999f;
    
    #if !DISABLESTEAMWORKS
    protected Callback<GameOverlayActivated_t> overlayCallback;
    #endif
    
    private void Start()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(Resume);
        
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        
        if (quitButton != null)
            quitButton.onClick.AddListener(QuitGame);
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (playerInput == null)
            playerInput = FindObjectOfType<PlayerInput>();
    }
    
    private void OnEnable()
    {
        #if !DISABLESTEAMWORKS
        if (SteamManager.Initialized)
        {
            overlayCallback = Callback<GameOverlayActivated_t>.Create(OnSteamOverlayActivated);
            Debug.Log("[Steam] Overlay callback registered");
        }
        #endif
    }
    
    #if !DISABLESTEAMWORKS
    private void OnSteamOverlayActivated(GameOverlayActivated_t callback)
    {
        // m_bActive: 1 = overlay açıldı, 0 = overlay kapandı
        if (callback.m_bActive != 0)
        {
            Debug.Log("[Steam] Overlay opened - pausing game");
            ForcePause();
        }
        // Overlay kapandığında resume YAPMIYORUZ - kullanıcı manuel kapatır
    }
    #endif
    
    private void Update()
    {
        if (Time.unscaledTime - lastInputTime < inputCooldown)
            return;

        bool pausePressed = Input.GetKeyDown(KeyCode.Escape);
        
        if (Gamepad.current != null)
        {
            if (Gamepad.current.startButton.wasPressedThisFrame)
                pausePressed = true;
            
            if (isPaused && Gamepad.current.buttonEast.wasPressedThisFrame)
            {
                lastInputTime = Time.unscaledTime;
                Resume();
                return;
            }
        }

        if (pausePressed)
        {
            lastInputTime = Time.unscaledTime;
            
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }
    
    private void Pause()
    {
        if (isPaused) return;
        
        Debug.Log("=== PAUSING ===");
        isPaused = true;
        
        if (blurBackground != null)
            blurBackground.CaptureAndBlur();
        
        if (playerInput != null)
            playerInput.enabled = false;
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(true);
        
        if (settingsManager != null)
            settingsManager.OpenSettings();
        
        if (pauseGameWhenOpen)
            Time.timeScale = 0f;
        
        if (firstSelected != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }
    
    public void Resume()
    {
        if (!isPaused) return;

        if (blurBackground != null)
            blurBackground.ClearBlur();
        
        Debug.Log("=== RESUMING ===");
        isPaused = false;
        
        if (settingsManager != null)
            settingsManager.CloseSettings();
        
        if (pauseMenuPanel != null)
            pauseMenuPanel.SetActive(false);
        
        if (pauseGameWhenOpen)
            Time.timeScale = 1f;
        
        if (playerInput != null)
            playerInput.enabled = true;
    }
    
    private void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        Time.timeScale = 1f;
        isPaused = false;
        
        if (playerInput != null)
            playerInput.enabled = true;
        
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    private void QuitGame()
    {
        Debug.Log("Quitting game...");
        Time.timeScale = 1f;
        
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    
    private void OnDestroy()
    {
        if (Time.timeScale != 1f)
            Time.timeScale = 1f;
    }
    
    private void OnApplicationQuit()
    {
        if (Time.timeScale != 1f)
            Time.timeScale = 1f;
    }

    public void ForcePause()
    {
        if (isPaused) return;
        Pause();
    }
}