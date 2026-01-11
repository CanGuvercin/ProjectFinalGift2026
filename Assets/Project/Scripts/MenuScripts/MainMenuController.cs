using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Menu Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button exitButton;
    
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject exitConfirmPanel;
    
    private void Start()
    {
        // Options ve Exit confirm panelleri başta kapalı
        if (optionsPanel != null) optionsPanel.SetActive(false);
        if (exitConfirmPanel != null) exitConfirmPanel.SetActive(false);
        
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
        // PlayerPrefs'te GameState var mı?
        bool hasSave = PlayerPrefs.HasKey("GameState");
        
        if (hasSave)
        {
            // Save var - Continue aktif
            continueButton.interactable = true;
            
            // Optional: Buton rengini normal yap
            ColorBlock colors = continueButton.colors;
            colors.normalColor = Color.white;
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            continueButton.colors = colors;
            
            Debug.Log("[MainMenu] Save found! Continue button enabled.");
        }
        else
        {
            // Save yok - Continue inactive
            continueButton.interactable = false;
            
            Debug.Log("[MainMenu] No save found. Continue button disabled.");
        }
    }
    
    // === BUTTON CALLBACKS ===
    
    public void OnContinue()
    {
        Debug.Log("Seni şimdi loading ekranının o siyah sularına bırakıyorum hehehe");
        
        // WorldMap scene'i yükle (CutsceneChief otomatik state'i yükler)
        LoadingManager.LoadScene("WorldMap");
    }
    
    public void OnNewGame()
    {
        Debug.Log("[MainMenu] New Game clicked - Resetting save...");
        
        // Confirmation popup göster (opsiyonel)
        if (PlayerPrefs.HasKey("GameState"))
        {
            // TODO: "Mevcut kayıt silinecek, emin misin?" popup
            // Şimdilik direkt reset
        }
        
        // Save'i sıfırla
        PlayerPrefs.DeleteKey("GameState");
        PlayerPrefs.SetInt("GameState", 0);
        PlayerPrefs.Save();
        
        // WorldMap'i yükle (State 0'dan başlar)
        LoadingManager.LoadScene("WorldMap");
    }
    
    public void OnOptions()
    {
        Debug.Log("[MainMenu] Options clicked");
        
        // Main menu'yü gizle, options'ı göster
        mainMenuPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
    
    public void OnExit()
    {
        Debug.Log("[MainMenu] Exit clicked - Showing confirmation");
        
        // Confirmation popup göster
        if (exitConfirmPanel != null)
        {
            exitConfirmPanel.SetActive(true);
        }
        else
        {
            // Popup yoksa direkt çık
            QuitGame();
        }
    }
    
    // Options'dan geri dön
    public void OnBackFromOptions()
    {
        optionsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }
    
    // Exit confirmation - Yes
    public void OnExitConfirm()
    {
        QuitGame();
    }
    
    // Exit confirmation - No
    public void OnExitCancel()
    {
        exitConfirmPanel.SetActive(false);
    }
    
    private void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting game...");
        
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
        //end
    }
}