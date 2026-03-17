using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class ControllerDisconnectHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PauseMenuManager pauseMenuManager;
    [SerializeField] private GameObject disconnectUI;
    [SerializeField] private CanvasGroup disconnectCanvasGroup;
    
    [Header("Settings")]
    [SerializeField] private float messageDuration = 6f;
    
    private Coroutine hideCoroutine;
    
    private void Start()
    {
        if (pauseMenuManager == null)
            pauseMenuManager = FindObjectOfType<PauseMenuManager>();
        
        if (disconnectUI != null)
            disconnectUI.SetActive(false);
        
        // CanvasGroup yoksa otomatik ekle
        if (disconnectCanvasGroup == null && disconnectUI != null)
            disconnectCanvasGroup = disconnectUI.GetComponent<CanvasGroup>();
        
        InputSystem.onDeviceChange += OnDeviceChange;
    }
    
    private void OnDestroy()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }
    
    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (device is Gamepad)
        {
            if (change == InputDeviceChange.Removed || change == InputDeviceChange.Disconnected)
            {
                Debug.Log("[Controller] Gamepad disconnected!");
                OnControllerDisconnected();
            }
            else if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
            {
                Debug.Log("[Controller] Gamepad reconnected!");
                OnControllerReconnected();
            }
        }
    }
    
    private void OnControllerDisconnected()
    {
        if (pauseMenuManager != null)
            pauseMenuManager.ForcePause();
        
        ShowDisconnectMessage();
    }
    
    private void OnControllerReconnected()
    {
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        
        if (disconnectUI != null)
            disconnectUI.SetActive(false);
    }
    
    private void ShowDisconnectMessage()
    {
        if (disconnectUI == null) return;
        
        if (hideCoroutine != null)
            StopCoroutine(hideCoroutine);
        
        disconnectUI.SetActive(true);
        
        if (disconnectCanvasGroup != null)
            disconnectCanvasGroup.alpha = 1f;
        
        hideCoroutine = StartCoroutine(ShowThenFadeOut());
    }
    
    private IEnumerator ShowThenFadeOut()
    {
        float halfDuration = messageDuration / 2f;
        
        // İlk yarı: Tam görünür bekle
        yield return new WaitForSecondsRealtime(halfDuration);
        
        // İkinci yarı: Fade out
        float elapsed = 0f;
        
        while (elapsed < halfDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / halfDuration);
            
            if (disconnectCanvasGroup != null)
                disconnectCanvasGroup.alpha = alpha;
            
            yield return null;
        }
        
        // Tamamen gizle
        if (disconnectUI != null)
            disconnectUI.SetActive(false);
        
        if (disconnectCanvasGroup != null)
            disconnectCanvasGroup.alpha = 1f; // Reset for next time
    }
}