using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class ControlsTabSwitcher : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject[] controlPanels; // Keyboard, Xbox, PlayStation
    
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleLabel;
    [SerializeField] private Image[] dotIndicators;
    
    [Header("Settings")]
    [SerializeField] private string[] panelNames = { "Keyboard", "Xbox", "PlayStation" };
    [SerializeField] private Color activeDotColor = Color.white;
    [SerializeField] private Color inactiveDotColor = new Color(1f, 1f, 1f, 0.3f);
    
    private int currentIndex = 0;//
    
    private void OnEnable()
    {
        currentIndex = 0;
        UpdateDisplay();
    }
    
    private void Update()
    {
        if (Gamepad.current == null) return;
        
        if (Gamepad.current.leftTrigger.wasPressedThisFrame)
        {
            Navigate(-1);
        }
        else if (Gamepad.current.rightTrigger.wasPressedThisFrame)
        {
            Navigate(1);
        }
    }
    
    private void Navigate(int direction)
    {
        currentIndex += direction;
        
        if (currentIndex < 0)
            currentIndex = controlPanels.Length - 1;
        else if (currentIndex >= controlPanels.Length)
            currentIndex = 0;
        
        UpdateDisplay();
    }
    
    private void UpdateDisplay()
    {
        // Panelleri güncelle
        for (int i = 0; i < controlPanels.Length; i++)
        {
            controlPanels[i].SetActive(i == currentIndex);
        }
        
        // Title güncelle
        if (titleLabel != null)
            titleLabel.text = panelNames[currentIndex];
        
        // Dot indicator güncelle
        for (int i = 0; i < dotIndicators.Length; i++)
        {
            dotIndicators[i].color = (i == currentIndex) ? activeDotColor : inactiveDotColor;
        }
    }
    
    // Mouse/keyboard için public metodlar
    public void GoToPanel(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, controlPanels.Length - 1);
        UpdateDisplay();
    }
    
    public void NextPanel() => Navigate(1);
    public void PreviousPanel() => Navigate(-1);
}