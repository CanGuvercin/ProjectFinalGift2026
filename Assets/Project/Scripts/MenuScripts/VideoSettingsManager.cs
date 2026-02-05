using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class VideoSettingsManager : MonoBehaviour
{
    [Header("Resolution")]
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    
    [Header("Quality")]
    [SerializeField] private TMP_Dropdown qualityDropdown;
    
    [Header("VSync")]
    [SerializeField] private Toggle vsyncToggle;
    
    [Header("Fullscreen")]
    [SerializeField] private Toggle fullscreenToggle;
    
    [Header("Apply Button")]
    [SerializeField] private Button applyButton;
    
    // FIXED: Sadece 16:9 standart çözünürlükler
    private struct ResolutionOption
    {
        public int width;
        public int height;
        public string displayName;
        
        public ResolutionOption(int w, int h)
        {
            width = w;
            height = h;
            displayName = $"{w} x {h}";
        }
    }
    
    private List<ResolutionOption> supportedResolutions = new List<ResolutionOption>
    {
        new ResolutionOption(1280, 720),    // HD
        new ResolutionOption(1920, 1080),   // Full HD (Default)
        new ResolutionOption(2560, 1440),   // QHD
        new ResolutionOption(3840, 2160)    // 4K
    };
    
    private void Start()
    {
        SetupResolutions();
        SetupQuality();
        SetupVSync();
        SetupFullscreen();
        
        if (applyButton != null)
        {
            applyButton.onClick.AddListener(ApplySettings);
        }
        
        LoadSettings();
        
        Debug.Log("[VideoSettings] Initialized");
    }
    
    private void SetupResolutions()
    {
        if (resolutionDropdown == null)
        {
            Debug.LogWarning("[VideoSettings] Resolution dropdown not assigned!");
            return;
        }
        
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        
        // Dropdown seçeneklerini ekle
        foreach (var res in supportedResolutions)
        {
            options.Add(res.displayName);
        }
        
        resolutionDropdown.AddOptions(options);
        
        // Şu anki çözünürlüğe en yakın olanı bul
        int currentIndex = FindClosestResolutionIndex();
        resolutionDropdown.value = currentIndex;
        resolutionDropdown.RefreshShownValue();
        
        Debug.Log($"[VideoSettings] Resolution initialized to: {supportedResolutions[currentIndex].displayName}");
    }
    
    private int FindClosestResolutionIndex()
    {
        int currentWidth = Screen.width;
        int currentHeight = Screen.height;
        
        // Tam eşleşme var mı?
        for (int i = 0; i < supportedResolutions.Count; i++)
        {
            if (supportedResolutions[i].width == currentWidth && 
                supportedResolutions[i].height == currentHeight)
            {
                return i;
            }
        }
        
        // Tam eşleşme yoksa en yakınını bul
        int closestIndex = 1; // Default: 1920x1080
        int minDifference = int.MaxValue;
        
        for (int i = 0; i < supportedResolutions.Count; i++)
        {
            int widthDiff = Mathf.Abs(supportedResolutions[i].width - currentWidth);
            int heightDiff = Mathf.Abs(supportedResolutions[i].height - currentHeight);
            int totalDiff = widthDiff + heightDiff;
            
            if (totalDiff < minDifference)
            {
                minDifference = totalDiff;
                closestIndex = i;
            }
        }
        
        return closestIndex;
    }
    
    private void SetupQuality()
    {
        if (qualityDropdown == null)
        {
            Debug.LogWarning("[VideoSettings] Quality dropdown not assigned!");
            return;
        }
        
        qualityDropdown.ClearOptions();
        
        // SADECE 2 SEVIYE - Pixel art için yeterli
        List<string> options = new List<string> { "Normal", "High" };
        
        qualityDropdown.AddOptions(options);
        
        // Mevcut ayarı yükle (0 = Normal, 1 = High)
        int savedQuality = PlayerPrefs.GetInt("CustomQuality", 1); // Default: High
        qualityDropdown.value = savedQuality;
        qualityDropdown.RefreshShownValue();
        
        Debug.Log($"[VideoSettings] Quality: {options[savedQuality]}");
    }
    
    private void SetupVSync()
    {
        if (vsyncToggle == null)
        {
            Debug.LogWarning("[VideoSettings] VSync toggle not assigned!");
            return;
        }
        
        vsyncToggle.onValueChanged.AddListener((value) => {
            Debug.Log($"[VideoSettings] VSync toggle changed to: {value}");
        });
    }
    
    private void SetupFullscreen()
    {
        if (fullscreenToggle == null)
        {
            Debug.LogWarning("[VideoSettings] Fullscreen toggle not assigned!");
            return;
        }
        
        fullscreenToggle.onValueChanged.AddListener((value) => {
            Debug.Log($"[VideoSettings] Fullscreen toggle changed to: {value}");
        });
    }
    
    public void ApplySettings()
    {
        Debug.Log("[VideoSettings] ========== APPLYING SETTINGS ==========");
        
        // Resolution
        if (resolutionDropdown != null)
        {
            int resIndex = resolutionDropdown.value;
            if (resIndex >= 0 && resIndex < supportedResolutions.Count)
            {
                ResolutionOption res = supportedResolutions[resIndex];
                bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
                
                Screen.SetResolution(res.width, res.height, isFullscreen);
                Debug.Log($"[VideoSettings] ✅ Resolution set to: {res.displayName}, Fullscreen: {isFullscreen}");
            }
        }
        
        // Quality - Pixel art için basit ayarlar
        if (qualityDropdown != null)
        {
            int qualityIndex = qualityDropdown.value;
            
            if (qualityIndex == 0) // Normal
            {
                Application.targetFrameRate = 60;
                QualitySettings.antiAliasing = 0;
                Debug.Log($"[VideoSettings] ✅ Quality: Normal (60 FPS, No AA)");
            }
            else // High
            {
                Application.targetFrameRate = -1;
                QualitySettings.antiAliasing = 0;
                Debug.Log($"[VideoSettings] ✅ Quality: High (Unlimited FPS, No AA)");
            }
            
            PlayerPrefs.SetInt("CustomQuality", qualityIndex);
        }
        
        // VSync
        if (vsyncToggle != null)
        {
            int vsyncCount = vsyncToggle.isOn ? 1 : 0;
            QualitySettings.vSyncCount = vsyncCount;
            Debug.Log($"[VideoSettings] ✅ VSync set to: {vsyncCount}");
        }
        
        // Fullscreen
        if (fullscreenToggle != null)
        {
            Screen.fullScreen = fullscreenToggle.isOn;
            Debug.Log($"[VideoSettings] ✅ Fullscreen set to: {fullscreenToggle.isOn}");
        }
        
        // Save
        SaveSettings();
        
        Debug.Log("[VideoSettings] ========== SETTINGS APPLIED ==========");
    }
    
    public void SaveSettings()
    {
        if (resolutionDropdown != null)
            PlayerPrefs.SetInt("ResolutionIndex", resolutionDropdown.value);
        
        if (qualityDropdown != null)
            PlayerPrefs.SetInt("CustomQuality", qualityDropdown.value);
        
        if (vsyncToggle != null)
            PlayerPrefs.SetInt("VSync", vsyncToggle.isOn ? 1 : 0);
        
        if (fullscreenToggle != null)
            PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        
        PlayerPrefs.Save();
        Debug.Log("[VideoSettings] 💾 Settings saved");
    }
    
    public void LoadSettings()
    {
        Debug.Log("[VideoSettings] 📂 Loading settings...");
        
        // Resolution
        if (resolutionDropdown != null)
        {
            int resIndex = PlayerPrefs.GetInt("ResolutionIndex", 1); // Default: 1920x1080
            if (resIndex >= 0 && resIndex < supportedResolutions.Count)
            {
                resolutionDropdown.value = resIndex;
                resolutionDropdown.RefreshShownValue();
            }
        }
        
        // Quality
        if (qualityDropdown != null)
        {
            int qualityIndex = PlayerPrefs.GetInt("CustomQuality", 1); // Default: High
            qualityDropdown.value = qualityIndex;
            qualityDropdown.RefreshShownValue();
            
            // FPS limit uygula
            if (qualityIndex == 0)
                Application.targetFrameRate = 60;
            else
                Application.targetFrameRate = -1;
        }
        
        // VSync
        if (vsyncToggle != null)
        {
            bool vsync = PlayerPrefs.GetInt("VSync", 0) == 1; // Default: kapalı
            vsyncToggle.isOn = vsync;
            QualitySettings.vSyncCount = vsync ? 1 : 0;
        }
        
        // Fullscreen
        if (fullscreenToggle != null)
        {
            bool fullscreen = PlayerPrefs.GetInt("Fullscreen", Screen.fullScreen ? 1 : 0) == 1;
            fullscreenToggle.isOn = fullscreen;
            Screen.fullScreen = fullscreen;
        }
        
        Debug.Log("[VideoSettings] ✅ Settings loaded");
    }
    
    [ContextMenu("Apply Settings (Debug)")]
    private void DebugApply()
    {
        ApplySettings();
    }
    
    [ContextMenu("Save Settings (Debug)")]
    private void DebugSave()
    {
        SaveSettings();
    }
    
    [ContextMenu("Load Settings (Debug)")]
    private void DebugLoad()
    {
        LoadSettings();
    }
}