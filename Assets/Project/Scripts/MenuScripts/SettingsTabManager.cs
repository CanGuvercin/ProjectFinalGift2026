using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsTabManager : MonoBehaviour
{
    [Header("Tab Buttons")]
    [SerializeField] private Button audioTabButton;
    [SerializeField] private Button gameplayTabButton;
    [SerializeField] private Button videoTabButton;
    [SerializeField] private Button controlsTabButton;
    
    [Header("Tab Content Panels")]
    [SerializeField] private GameObject audioTabContent;
    [SerializeField] private GameObject gameplayTabContent;
    [SerializeField] private GameObject videoTabContent;
    [SerializeField] private GameObject controlsTabContent;
    
    [Header("Tab Colors")]
    [SerializeField] private Color activeTabColor = new Color(1f, 0.9f, 0.7f, 1f); // Açık kahve
    [SerializeField] private Color inactiveTabColor = new Color(0.6f, 0.5f, 0.4f, 1f); // Koyu kahve
    
    // ⚡ YENİ: CanvasGroup cache - HIZLI GEÇİŞ İÇİN
    private CanvasGroup audioCanvasGroup;
    private CanvasGroup gameplayCanvasGroup;
    private CanvasGroup videoCanvasGroup;
    private CanvasGroup controlsCanvasGroup;
    
    private TabType currentTab = TabType.Audio;
    private bool isInitialized = false; // YENİ: İlk başlatma kontrolü
    
    private void Awake()
    {
        // ⚡ CanvasGroup'ları ekle/al - HIZLI GEÇİŞ İÇİN
        audioCanvasGroup = GetOrAddCanvasGroup(audioTabContent);
        gameplayCanvasGroup = GetOrAddCanvasGroup(gameplayTabContent);
        videoCanvasGroup = GetOrAddCanvasGroup(videoTabContent);
        controlsCanvasGroup = GetOrAddCanvasGroup(controlsTabContent);
        
        // ÖNEMLİ: Tüm content'leri aktif bırak - CanvasGroup ile kontrol edeceğiz
        audioTabContent.SetActive(true);
        gameplayTabContent.SetActive(true);
        videoTabContent.SetActive(true);
        controlsTabContent.SetActive(true);
        
        Debug.Log("[SettingsTabManager] ⚡ Fast tab system initialized!");
    }
    
    private void Start()
    {
        // Button listeners
        audioTabButton.onClick.AddListener(() => SwitchTab(TabType.Audio));
        gameplayTabButton.onClick.AddListener(() => SwitchTab(TabType.Gameplay));
        videoTabButton.onClick.AddListener(() => SwitchTab(TabType.Video));
        controlsTabButton.onClick.AddListener(() => SwitchTab(TabType.Controls));
        
        // Başlangıçta Audio tab açık - DİĞERLERİ KAPALI
        InitializeDefaultTab();
    }
    
    // YENİ: İlk başlangıçta sadece Audio'yu göster
    private void InitializeDefaultTab()
    {
        // Önce hepsini gizle
        HideTab(audioCanvasGroup);
        HideTab(gameplayCanvasGroup);
        HideTab(videoCanvasGroup);
        HideTab(controlsCanvasGroup);
        
        // Tüm button'ları inactive yap
        SetButtonColor(audioTabButton, inactiveTabColor);
        SetButtonColor(gameplayTabButton, inactiveTabColor);
        SetButtonColor(videoTabButton, inactiveTabColor);
        SetButtonColor(controlsTabButton, inactiveTabColor);
        
        // Sadece Audio'yu göster
        ShowTab(audioCanvasGroup);
        SetButtonColor(audioTabButton, activeTabColor);
        
        currentTab = TabType.Audio;
        isInitialized = true;
        
        Debug.Log("[SettingsTabManager] ✅ Default tab (Audio) initialized");
    }
    
    public enum TabType
    {
        Audio,
        Gameplay,
        Video,
        Controls
    }
    
    public void SwitchTab(TabType tab)
    {
        // Aynı tab'a tıklanmışsa skip
        if (tab == currentTab && isInitialized)
        {
            Debug.Log($"[SettingsTabManager] Tab {tab} already active");
            return;
        }
        
        Debug.Log($"[SettingsTabManager] ⚡ INSTANT switch to {tab}");
        
        // ⚡ HIZLI: SetActive KULLANMA - CanvasGroup alpha kullan!
        HideTab(audioCanvasGroup);
        HideTab(gameplayCanvasGroup);
        HideTab(videoCanvasGroup);
        HideTab(controlsCanvasGroup);
        
        // Tüm button'ları inactive yap
        SetButtonColor(audioTabButton, inactiveTabColor);
        SetButtonColor(gameplayTabButton, inactiveTabColor);
        SetButtonColor(videoTabButton, inactiveTabColor);
        SetButtonColor(controlsTabButton, inactiveTabColor);
        
        // ⚡ Seçilen tab'ı ANINDA göster (CanvasGroup ile)
        switch(tab)
        {
            case TabType.Audio:
                ShowTab(audioCanvasGroup);
                SetButtonColor(audioTabButton, activeTabColor);
                break;
            case TabType.Gameplay:
                ShowTab(gameplayCanvasGroup);
                SetButtonColor(gameplayTabButton, activeTabColor);
                break;
            case TabType.Video:
                ShowTab(videoCanvasGroup);
                SetButtonColor(videoTabButton, activeTabColor);
                break;
            case TabType.Controls:
                ShowTab(controlsCanvasGroup);
                SetButtonColor(controlsTabButton, activeTabColor);
                break;
        }
        
        currentTab = tab;
        isInitialized = true;
    }
    
    // ⚡ YENİ: CanvasGroup helper metodlar - ANINDA göster/gizle
    private void ShowTab(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }
    
    private void HideTab(CanvasGroup canvasGroup)
    {
        if (canvasGroup == null) return;
        
        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }
    
    // ⚡ YENİ: CanvasGroup ekle veya al
    private CanvasGroup GetOrAddCanvasGroup(GameObject obj)
    {
        if (obj == null)
        {
            Debug.LogError("[SettingsTabManager] Tab content is NULL!");
            return null;
        }
        
        CanvasGroup cg = obj.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = obj.AddComponent<CanvasGroup>();
            Debug.Log($"[SettingsTabManager] Added CanvasGroup to {obj.name}");
        }
        
        return cg;
    }
    
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return;
        
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.selectedColor = color;
        button.colors = colors;
        
        // Text rengini de ayarla
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.color = color == activeTabColor ? Color.black : new Color(0.8f, 0.8f, 0.8f, 1f);
        }
    }
    
    #if UNITY_EDITOR
    [ContextMenu("Debug: Audio Tab")]
    private void DebugAudio() => SwitchTab(TabType.Audio);
    
    [ContextMenu("Debug: Gameplay Tab")]
    private void DebugGameplay() => SwitchTab(TabType.Gameplay);
    
    [ContextMenu("Debug: Video Tab")]
    private void DebugVideo() => SwitchTab(TabType.Video);
    
    [ContextMenu("Debug: Controls Tab")]
    private void DebugControls() => SwitchTab(TabType.Controls);
    #endif
}