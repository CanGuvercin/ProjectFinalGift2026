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
    
    private Resolution[] resolutions;
    private List<Resolution> filteredResolutions;
    
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
        
        resolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        
        resolutionDropdown.ClearOptions();
        List<string> options = new List<string>();
        
        int currentResolutionIndex = 0;
        
        // Sadece unique çözünürlükleri al (refresh rate farklılıklarını ignore et)
        for (int i = 0; i < resolutions.Length; i++)
        {
            // Duplicate kontrolü
            bool isDuplicate = false;
            foreach (Resolution res in filteredResolutions)
            {
                if (res.width == resolutions[i].width && res.height == resolutions[i].height)
                {
                    isDuplicate = true;
                    break;
                }
            }
            
            if (!isDuplicate)
            {
                filteredResolutions.Add(resolutions[i]);
                string option = resolutions[i].width + " x " + resolutions[i].height;
                options.Add(option);
                
                // Şu anki çözünürlüğü bul
                if (resolutions[i].width == Screen.width && resolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }
        
        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
        
        Debug.Log($"[VideoSettings] Found {filteredResolutions.Count} resolutions, current: {currentResolutionIndex}");
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
        if (resolutionDropdown != null && filteredResolutions.Count > 0)
        {
            int resIndex = resolutionDropdown.value;
            if (resIndex >= 0 && resIndex < filteredResolutions.Count)
            {
                Resolution resolution = filteredResolutions[resIndex];
                bool isFullscreen = fullscreenToggle != null ? fullscreenToggle.isOn : Screen.fullScreen;
                
                Screen.SetResolution(resolution.width, resolution.height, isFullscreen);
                Debug.Log($"[VideoSettings] ✅ Resolution set to: {resolution.width}x{resolution.height}, Fullscreen: {isFullscreen}");
            }
        }
        
        // Quality - Pixel art için basit ayarlar
        if (qualityDropdown != null)
        {
            int qualityIndex = qualityDropdown.value;
            
            if (qualityIndex == 0) // Normal
            {
                // Normal ayarlar
                Application.targetFrameRate = 60;
                QualitySettings.antiAliasing = 0; // Pixel art için AA kapalı
                Debug.Log($"[VideoSettings] ✅ Quality: Normal (60 FPS, No AA)");
            }
            else // High
            {
                // High ayarlar
                Application.targetFrameRate = -1; // Sınırsız FPS
                QualitySettings.antiAliasing = 0; // Pixel art için yine kapalı
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
        if (resolutionDropdown != null && PlayerPrefs.HasKey("ResolutionIndex"))
        {
            int resIndex = PlayerPrefs.GetInt("ResolutionIndex");
            if (resIndex >= 0 && resIndex < filteredResolutions.Count)
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