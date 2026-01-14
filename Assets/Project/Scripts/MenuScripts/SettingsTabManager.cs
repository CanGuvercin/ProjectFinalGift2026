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
    
    private void Start()
    {
        // Button listeners
        audioTabButton.onClick.AddListener(() => SwitchTab(TabType.Audio));
        gameplayTabButton.onClick.AddListener(() => SwitchTab(TabType.Gameplay));
        videoTabButton.onClick.AddListener(() => SwitchTab(TabType.Video));
        controlsTabButton.onClick.AddListener(() => SwitchTab(TabType.Controls));
        
        // Başlangıçta Audio tab açık
        SwitchTab(TabType.Audio);
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
        // Tüm tab'ları kapat
        audioTabContent.SetActive(false);
        gameplayTabContent.SetActive(false);
        videoTabContent.SetActive(false);
        controlsTabContent.SetActive(false);
        
        // Tüm button'ları inactive yap
        SetButtonColor(audioTabButton, inactiveTabColor);
        SetButtonColor(gameplayTabButton, inactiveTabColor);
        SetButtonColor(videoTabButton, inactiveTabColor);
        SetButtonColor(controlsTabButton, inactiveTabColor);
        
        // Seçilen tab'ı aç
        switch(tab)
        {
            case TabType.Audio:
                audioTabContent.SetActive(true);
                SetButtonColor(audioTabButton, activeTabColor);
                break;
            case TabType.Gameplay:
                gameplayTabContent.SetActive(true);
                SetButtonColor(gameplayTabButton, activeTabColor);
                break;
            case TabType.Video:
                videoTabContent.SetActive(true);
                SetButtonColor(videoTabButton, activeTabColor);
                break;
            case TabType.Controls:
                controlsTabContent.SetActive(true);
                SetButtonColor(controlsTabButton, activeTabColor);
                break;
        }
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
}