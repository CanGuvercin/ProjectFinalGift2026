using UnityEngine;

public class PauseMenuManager : MonoBehaviour
{
    [Header("Settings Manager")]
    [SerializeField] private SettingsManager settingsManager;
    
    [Header("Pause Settings")]
    [SerializeField] private bool pauseGameWhenOpen = true;
    
    private bool isPaused = false;
    
    private void Update()
    {
        // ESC tuşu
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (settingsManager == null)
            {
                return;
            }
            
            // Settings açık mı kontrol et
            bool isOpen = settingsManager.IsOpen();
            
            if (isOpen)
            {
                // Açık - kapat
                ClosePauseMenu();
            }
            else
            {
                // Kapalı - aç
                OpenPauseMenu();
            }
        }
    }
    
    private void OpenPauseMenu()
    {
        if (isPaused)
        {
            return;
        }
        isPaused = true;
        
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
    
    private void ClosePauseMenu()
    {
        if (!isPaused)
        {
            return;
        }
        isPaused = false;
        
        // Settings'i kapat
        if (settingsManager != null)
        {
            settingsManager.CloseSettings();
        }
        
        // Oyunu devam ettir
        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }
    
    private void OnDestroy()
    {
        // TimeScale'i resetle
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