using UnityEngine;
using UnityEngine.UI;

public class SettingsManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button applyButton;
    
    [Header("Tab Manager")]
    [SerializeField] private SettingsTabManager tabManager;
    
    [Header("Audio Manager")]
    [SerializeField] private AudioSettingsManager audioManager;
    
    private void Start()
    {
        // Button listeners - direkt kapatır
        if (closeButton != null)
            closeButton.onClick.AddListener(() => CloseSettings());
        
        if (backButton != null)
            backButton.onClick.AddListener(() => CloseSettings());
        
        if (applyButton != null)
            applyButton.onClick.AddListener(ApplySettings);
    }
    
    public void OpenSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            // Load current settings
            if (audioManager != null)
                audioManager.LoadSettings();
        }
    }
    
    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }
    
    public void ApplySettings()
    {
        // Save audio settings
        if (audioManager != null)
            audioManager.SaveSettings();
    }
    
    public bool IsOpen()
    {
        return settingsPanel != null && settingsPanel.activeSelf;
    }
}