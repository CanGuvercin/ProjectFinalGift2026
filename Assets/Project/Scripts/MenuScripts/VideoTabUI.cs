using UnityEngine;
using UnityEngine.UI;

public class VideoTabUI : MonoBehaviour
{
    [Header("Graphics Toggles")]
    [SerializeField] private Toggle fullscreenToggle;
    [SerializeField] private Toggle vSyncToggle;

    private void OnEnable()
    {
        // Panel açıldığında mevcut ayarları yükle
        LoadCurrentSettings();
        
        // Listener'ları ekle
        AddListeners();
    }

    private void OnDisable()
    {
        // Listener'ları temizle
        RemoveListeners();
    }

    private void AddListeners()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
        
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.AddListener(OnVSyncChanged);
    }

    private void RemoveListeners()
    {
        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);
        
        if (vSyncToggle != null)
            vSyncToggle.onValueChanged.RemoveListener(OnVSyncChanged);
    }

    private void LoadCurrentSettings()
    {
        if (GameplayManager.Instance == null)
        {
            Debug.LogWarning("VideoTabUI: GameplayManager not found!");
            return;
        }

        // Listener'ları tetiklememek için geçici olarak kaldır
        RemoveListeners();

        // Mevcut ayarları GameplayManager'dan al ve UI'ya yansıt
        if (fullscreenToggle != null)
            fullscreenToggle.isOn = GameplayManager.Instance.GetFullscreenEnabled();
        
        if (vSyncToggle != null)
            vSyncToggle.isOn = GameplayManager.Instance.GetVSyncEnabled();

        // Listener'ları geri ekle
        AddListeners();
    }

    // Fullscreen toggle değiştiğinde
    private void OnFullscreenChanged(bool isOn)
    {
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.SetFullscreen(isOn);
        }
    }

    // VSync toggle değiştiğinde
    private void OnVSyncChanged(bool isOn)
    {
        if (GameplayManager.Instance != null)
        {
            GameplayManager.Instance.SetVSync(isOn);
        }
    }
}
