using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    
    [Header("Pause Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private float inputCooldown = 0.2f;
    
    private bool isPaused = false;
    private float lastInputTime = -999f;
    
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
    }
    
    private void Update()
    {
        // ESC tuşu - tek toggle
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Cooldown kontrolü
            if (Time.unscaledTime - lastInputTime < inputCooldown)
            {
                return;
            }
            
            lastInputTime = Time.unscaledTime;
            
            // Basit toggle: açıksa kapat, kapalıysa aç
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }
    
    private void Pause()
    {
        if (isPaused) return;
        
        Debug.Log("=== PAUSING ===");
        isPaused = true;
        
        // Ana pause panel'i aç
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(true);
        }
        
        // Settings'i aç
        if (settingsManager != null)
        {
            settingsManager.OpenSettings();
        }
        
        // Oyunu duraklat
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }
    }
    
    public void Resume()
    {
        if (!isPaused) return;
        
        Debug.Log("=== RESUMING ===");
        isPaused = false;
        
        // Settings'i kapat
        if (settingsManager != null)
        {
            settingsManager.CloseSettings();
        }
        
        // Ana pause panel'i kapat
        if (pauseMenuPanel != null)
        {
            pauseMenuPanel.SetActive(false);
        }
        
        // Oyunu devam ettir
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }
    
    private void ReturnToMainMenu()
    {
        Debug.Log("Returning to main menu...");
        Time.timeScale = 1f;
        isPaused = false;
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
        {
            Time.timeScale = 1f;
        }
    }
    
    private void OnApplicationQuit()
    {
        if (Time.timeScale != 1f)
        {
            Time.timeScale = 1f;
        }
    }
}