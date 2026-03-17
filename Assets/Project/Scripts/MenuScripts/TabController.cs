using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TabController : MonoBehaviour
{
    [SerializeField] private Button[] tabButtons;
    [SerializeField] private Selectable[] firstContentElements;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button backButton;
    
    private int currentTabIndex = 0;

    private void OnEnable()
    {
        currentTabIndex = 0;
        SelectFirstContent();
    }

    private void Update()
    {
        if (Gamepad.current == null) return;

        // Tab switching
        if (Gamepad.current.rightShoulder.wasPressedThisFrame)
            SwitchTab(1);
        else if (Gamepad.current.leftShoulder.wasPressedThisFrame)
            SwitchTab(-1);

        // Apply = South button (A/X)
        if (Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            // Sadece bir şey seçili değilse veya footer'da değilsek Apply'a basma
            // Bu kısım opsiyonel - şimdilik kaldıralım
        }

        // Back = East button (B/O)
        if (Gamepad.current.buttonEast.wasPressedThisFrame)
        {
            backButton.onClick.Invoke();
        }
    }

    private void SwitchTab(int direction)
    {
        currentTabIndex += direction;
        
        if (currentTabIndex >= tabButtons.Length)
            currentTabIndex = 0;
        else if (currentTabIndex < 0)
            currentTabIndex = tabButtons.Length - 1;

        tabButtons[currentTabIndex].onClick.Invoke();
        SelectFirstContent();
    }

    private void SelectFirstContent()
    {
        if (firstContentElements[currentTabIndex] != null)
            EventSystem.current.SetSelectedGameObject(firstContentElements[currentTabIndex].gameObject);
    }
}