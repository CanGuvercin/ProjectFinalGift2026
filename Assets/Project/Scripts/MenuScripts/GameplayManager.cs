using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance { get; private set; }

    [Header("Difficulty Settings")]
    [SerializeField] private DifficultyLevel currentDifficulty = DifficultyLevel.Normal;
    [SerializeField] private TMP_Dropdown difficultyDropdown; // YENİ: Dropdown referansı

    [Header("Camera Shake Reference")]
    [SerializeField] private CameraShake cameraShake;
    
    [Header("Graphics Settings")]
    [SerializeField] private bool vSyncEnabled = true;
    [SerializeField] private bool fullscreenEnabled = true;

    public enum DifficultyLevel
    {
        Easy,
        Normal,
        Hard
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeCameraShake();
        LoadSettings();
        ApplyGraphicsSettings();
        
        // Dropdown setup
        SetupDifficultyDropdown();
    }

    private void InitializeCameraShake()
    {
        if (cameraShake == null)
        {
            cameraShake = FindObjectOfType<CameraShake>();
        }
    }

    // YENİ: Dropdown setup
    private void SetupDifficultyDropdown()
    {
        if (difficultyDropdown == null)
        {
            Debug.LogWarning("[GameplayManager] Difficulty dropdown not assigned!");
            return;
        }

        // Dropdown'ı temizle ve seçenekleri ekle
        difficultyDropdown.ClearOptions();
        difficultyDropdown.AddOptions(new System.Collections.Generic.List<string> 
        { 
            "Easy", 
            "Normal", 
            "Hard" 
        });

        // Mevcut zorluk seviyesini dropdown'a yansıt
        difficultyDropdown.value = (int)currentDifficulty;
        difficultyDropdown.RefreshShownValue();

        // Dropdown değiştiğinde çağrılacak
        difficultyDropdown.onValueChanged.AddListener(OnDifficultyDropdownChanged);

        Debug.Log($"[GameplayManager] Difficulty dropdown initialized to: {currentDifficulty}");
    }

    // YENİ: Dropdown değiştiğinde
    private void OnDifficultyDropdownChanged(int index)
    {
        DifficultyLevel newDifficulty = (DifficultyLevel)index;
        SetDifficulty(newDifficulty);
        
        Debug.Log($"[GameplayManager] 🎮 Difficulty changed via dropdown to: {newDifficulty}");
        Debug.Log($"[GameplayManager] 📊 Incoming damage multiplier: {GetIncomingDamageMultiplier()}x");
        Debug.Log($"[GameplayManager] 📊 Outgoing damage multiplier: {GetOutgoingDamageMultiplier()}x");
    }

    // YENİ: Dropdown'ı manuel güncelle (opsiyonel)
    public void UpdateDifficultyDropdown()
    {
        if (difficultyDropdown != null)
        {
            difficultyDropdown.SetValueWithoutNotify((int)currentDifficulty);
            difficultyDropdown.RefreshShownValue();
        }
    }

    #region ScreenShake Management

    public void SetScreenShakeMode(CameraShake.ShakeMode mode)
    {
        if (cameraShake == null) return;
        cameraShake.SetShakeMode(mode);
    }

    public CameraShake.ShakeMode GetScreenShakeMode()
    {
        if (cameraShake == null) return CameraShake.ShakeMode.Normal;
        return cameraShake.GetShakeMode();
    }

    public bool IsScreenShakeEnabled()
    {
        return GetScreenShakeMode() == CameraShake.ShakeMode.Normal;
    }

    public void ToggleScreenShake()
    {
        var currentMode = GetScreenShakeMode();
        var newMode = currentMode == CameraShake.ShakeMode.Normal 
            ? CameraShake.ShakeMode.NoShake 
            : CameraShake.ShakeMode.Normal;
        
        SetScreenShakeMode(newMode);
    }

    #endregion

    #region Difficulty Management

    public void SetDifficulty(DifficultyLevel difficulty)
    {
        currentDifficulty = difficulty;
        SaveSettings();
        
        Debug.Log($"[GameplayManager] ⚙️ Difficulty set to: {difficulty}");
    }

    public DifficultyLevel GetDifficulty()
    {
        return currentDifficulty;
    }

    public float GetIncomingDamageMultiplier()
    {
        return currentDifficulty switch
        {
            DifficultyLevel.Easy => 0.6f,    // %40 daha az hasar alırsın
            DifficultyLevel.Normal => 1.0f,  // Normal hasar
            DifficultyLevel.Hard => 1.4f,    // %40 daha fazla hasar alırsın
            _ => 1.0f
        };
    }

    public float GetOutgoingDamageMultiplier()
    {
        return currentDifficulty switch
        {
            DifficultyLevel.Easy => 1.4f,    // %40 daha fazla hasar verirsin
            DifficultyLevel.Normal => 1.0f,  // Normal hasar
            DifficultyLevel.Hard => 0.7f,    // %30 daha az hasar verirsin
            _ => 1.0f
        };
    }

    public string GetDifficultyName()
    {
        return currentDifficulty switch
        {
            DifficultyLevel.Easy => "Kolay",
            DifficultyLevel.Normal => "Normal",
            DifficultyLevel.Hard => "Zor",
            _ => "Normal"
        };
    }

    #endregion

    #region Graphics Management

    public void SetVSync(bool enabled)
    {
        vSyncEnabled = enabled;
        QualitySettings.vSyncCount = enabled ? 1 : 0;
        SaveSettings();
    }

    public bool GetVSyncEnabled()
    {
        return vSyncEnabled;
    }

    public void SetFullscreen(bool enabled)
    {
        fullscreenEnabled = enabled;
        Screen.fullScreen = enabled;
        SaveSettings();
    }

    public bool GetFullscreenEnabled()
    {
        return fullscreenEnabled;
    }

    private void ApplyGraphicsSettings()
    {
        // VSync
        QualitySettings.vSyncCount = vSyncEnabled ? 1 : 0;
        
        // Fullscreen
        Screen.fullScreen = fullscreenEnabled;
    }

    #endregion

    #region Settings Persistence

    private void SaveSettings()
    {
        // Difficulty
        PlayerPrefs.SetInt("Difficulty", (int)currentDifficulty);
        
        // Graphics
        PlayerPrefs.SetInt("VSync", vSyncEnabled ? 1 : 0);
        PlayerPrefs.SetInt("Fullscreen", fullscreenEnabled ? 1 : 0);
        
        PlayerPrefs.Save();
        
        Debug.Log($"[GameplayManager] 💾 Settings saved - Difficulty: {currentDifficulty}");
    }

    private void LoadSettings()
    {
        // Difficulty
        currentDifficulty = (DifficultyLevel)PlayerPrefs.GetInt("Difficulty", (int)DifficultyLevel.Normal);
        
        // Graphics
        vSyncEnabled = PlayerPrefs.GetInt("VSync", 1) == 1; // Default: ON
        fullscreenEnabled = PlayerPrefs.GetInt("Fullscreen", 1) == 1; // Default: ON
        
        Debug.Log($"[GameplayManager] 📂 Settings loaded - Difficulty: {currentDifficulty}");
    }

    public void ResetAllToDefaults()
    {
        // Difficulty
        currentDifficulty = DifficultyLevel.Normal;
        
        // Shake
        SetScreenShakeMode(CameraShake.ShakeMode.Normal);
        
        // Graphics
        vSyncEnabled = true;
        fullscreenEnabled = true;
        
        SaveSettings();
        ApplyGraphicsSettings();
        UpdateDifficultyDropdown();
    }

    #endregion

    #region Debug Commands

    [ContextMenu("Test - Toggle Shake")]
    private void TestToggleShake() => ToggleScreenShake();

    [ContextMenu("Test - Toggle VSync")]
    private void TestToggleVSync() => SetVSync(!vSyncEnabled);

    [ContextMenu("Test - Toggle Fullscreen")]
    private void TestToggleFullscreen() => SetFullscreen(!fullscreenEnabled);

    [ContextMenu("Test - Difficulty: Easy")]
    private void TestEasy() => SetDifficulty(DifficultyLevel.Easy);

    [ContextMenu("Test - Difficulty: Hard")]
    private void TestHard() => SetDifficulty(DifficultyLevel.Hard);
    
    [ContextMenu("Print Current Multipliers")]
    private void PrintMultipliers()
    {
        Debug.Log($"=== DIFFICULTY: {currentDifficulty} ===");
        Debug.Log($"Incoming Damage: {GetIncomingDamageMultiplier()}x");
        Debug.Log($"Outgoing Damage: {GetOutgoingDamageMultiplier()}x");
    }

    #endregion
}