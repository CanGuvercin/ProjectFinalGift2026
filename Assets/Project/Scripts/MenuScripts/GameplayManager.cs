using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    public static GameplayManager Instance { get; private set; }

    [Header("Difficulty Settings")]
    [SerializeField] private DifficultyLevel currentDifficulty = DifficultyLevel.Normal;

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
    }

    private void InitializeCameraShake()
    {
        if (cameraShake == null)
        {
            cameraShake = FindObjectOfType<CameraShake>();
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
    }

    public DifficultyLevel GetDifficulty()
    {
        return currentDifficulty;
    }

    public float GetIncomingDamageMultiplier()
    {
        return currentDifficulty switch
        {
            DifficultyLevel.Easy => 0.6f,
            DifficultyLevel.Normal => 1.0f,
            DifficultyLevel.Hard => 1.4f,
            _ => 1.0f
        };
    }

    public float GetOutgoingDamageMultiplier()
    {
        return currentDifficulty switch
        {
            DifficultyLevel.Easy => 1.4f,
            DifficultyLevel.Normal => 1.0f,
            DifficultyLevel.Hard => 0.7f,
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
    }

    private void LoadSettings()
    {
        // Difficulty
        currentDifficulty = (DifficultyLevel)PlayerPrefs.GetInt("Difficulty", (int)DifficultyLevel.Normal);
        
        // Graphics
        vSyncEnabled = PlayerPrefs.GetInt("VSync", 1) == 1; // Default: ON
        fullscreenEnabled = PlayerPrefs.GetInt("Fullscreen", 1) == 1; // Default: ON
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

    #endregion
}