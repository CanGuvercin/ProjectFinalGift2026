using UnityEngine;
using TMPro;
using System.Collections;

public class DialogBox : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private float displayDuration = 3f; // Otomatik kapanma süresi (0 = sonsuz)
    
    private static DialogBox instance;
    
    private void Awake()
    {
        instance = this;
        gameObject.SetActive(false); // Başta kapalı
    }
    
    // Mesaj göster (static - her yerden çağrılabilir)
    public static void Show(string message, float duration = 0f)
    {
        if (instance == null) return;
        
        instance.messageText.text = message;
        instance.gameObject.SetActive(true);
        
        // Otomatik kapat (duration > 0 ise)
        if (duration > 0f)
        {
            instance.StartCoroutine(instance.HideAfterDelay(duration));
        }
    }
    
    // Mesajı gizle
    public static void Hide()
    {
        if (instance == null) return;
        instance.gameObject.SetActive(false);
    }
    
    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Hide();
    }
}