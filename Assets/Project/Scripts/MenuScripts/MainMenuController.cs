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
    
    [Header("Settings Manager")]
    [SerializeField] private SettingsManager settingsManager;
    
    [Header("Settings Back Button")]
    [SerializeField] private Button settingsBackButton;
    
    private const string SAVE_KEY = "GameState";
    private const string SAVE_SCENE_KEY = "GameScene";
    
    private void Start()
    {
        if (settingsBackButton != null)
            settingsBackButton.onClick.AddListener(OnBackFromSettings);
        
        CheckSaveData();
        
        continueButton.onClick.AddListener(OnContinue);
        newGameButton.onClick.AddListener(OnNewGame);
        optionsButton.onClick.AddListener(OnOptions);
        exitButton.onClick.AddListener(OnExit);
    }
    
    private void CheckSaveData()
    {
        bool hasSave = PlayerPrefs.HasKey(SAVE_KEY);
        continueButton.interactable = hasSave;
        
        Debug.Log(hasSave
            ? "[MainMenu] ✅ Save found! Continue button enabled."
            : "[MainMenu] ❌ No save found. Continue button disabled.");
    }
    
    public void OnContinue()
    {
        Debug.Log("[MainMenu] Continue clicked - Loading saved game...");
        
        string savedScene = PlayerPrefs.GetString(SAVE_SCENE_KEY, "WorldMap");
        Debug.Log($"[MainMenu] Loading saved scene: {savedScene}");
        
        LoadingManager.LoadScene(savedScene);
    }
    
    public void OnNewGame()
    {
        Debug.Log("[MainMenu] New Game clicked - Clearing save data...");
        
        // ÖNEMLİ: Sadece sil, tekrar SetInt yapma!
        // SetInt yapılırsa key yeniden oluşur ve Continue aktifleşir.
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(SAVE_SCENE_KEY);
        PlayerPrefs.Save();
        
        // State 0'dan başlat, spawn noktası boş
        LoadingManager.LoadScene("WorldMap", 0, "");
        
        Debug.Log("[MainMenu] Save cleared. Starting from state 0.");
    }
    
    public void OnOptions()
    {
        Debug.Log("[MainMenu] Options clicked");
        
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(false);
        
        if (settingsManager != null)
            settingsManager.OpenSettings();
    }
    
    public void OnBackFromSettings()
    {
        Debug.Log("[MainMenu] Back from settings clicked");
        
        if (settingsManager != null)
            settingsManager.CloseSettings();
        
        if (mainMenuPanel != null)
            mainMenuPanel.SetActive(true);
    }
    
    public void OnExit()
    {
        Debug.Log("[MainMenu] Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}