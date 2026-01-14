using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject exitConfirmPanel;
    
    [Header("Settings Manager")]
    [SerializeField] private SettingsManager settingsManager;
    
    [Header("Settings Back Button")]
    [SerializeField] private Button settingsBackButton; // Settings içindeki Back button
    
    private void Start()
    {
        // Exit confirm paneli başta kapalı
        if (exitConfirmPanel != null) 
            exitConfirmPanel.SetActive(false);
        
        // Settings Back button listener
        if (settingsBackButton != null)
        {
            settingsBackButton.onClick.AddListener(OnBackFromSettings);
        }
        
        // Save var mı kontrol et
        CheckSaveData();
        
        // Button listener'lar
        continueButton.onClick.AddListener(OnContinue);
        newGameButton.onClick.AddListener(OnNewGame);
        optionsButton.onClick.AddListener(OnOptions);
        exitButton.onClick.AddListener(OnExit);
    }
    
    private void CheckSaveData()
    {
        bool hasSave = PlayerPrefs.HasKey("GameState");
        
        if (hasSave)
        {
            continueButton.interactable = true;
            Debug.Log("[MainMenu] Save found! Continue button enabled.");
        }
        else
        {
            continueButton.interactable = false;
            Debug.Log("[MainMenu] No save found. Continue button disabled.");
        }
    }
    
    public void OnContinue()
    {
        Debug.Log("[MainMenu] Continue clicked - Loading saved game...");
        LoadingManager.LoadScene("WorldMap");
    }
    
    public void OnNewGame()
    {
        Debug.Log("[MainMenu] New Game clicked - Resetting save...");
        
        PlayerPrefs.DeleteKey("GameState");
        PlayerPrefs.SetInt("GameState", 0);
        PlayerPrefs.Save();
        
        LoadingManager.LoadScene("WorldMap");
    }
    
    public void OnOptions()
    {
        Debug.Log("[MainMenu] Options clicked");
        
        // Main menu'yü gizle
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }
        
        // Settings'i aç
        if (settingsManager != null)
        {
            settingsManager.OpenSettings();
        }
    }
    
    // Settings'den geri dön
    public void OnBackFromSettings()
    {
        Debug.Log("[MainMenu] Back from settings clicked");
        
        // Settings'i kapat
        if (settingsManager != null)
        {
            settingsManager.CloseSettings();
        }
        
        // Main menu'yü göster
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }
    
    public void OnExit()
    {
        Debug.Log("[MainMenu] Exit clicked - Showing confirmation");
        
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(true);
        }
        else
        {
            QuitGame();
        }
    }
    
    public void OnExitConfirm()
    {
        QuitGame();
    }
    
    public void OnExitCancel()
    {
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(false);
        }
    }
    
    private void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}