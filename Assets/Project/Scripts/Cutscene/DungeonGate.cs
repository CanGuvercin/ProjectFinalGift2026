using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class DungeonGate : MonoBehaviour
{
    [Header("Dungeon Info")]
    [SerializeField] private string dungeonID = "Dungeon1";
    [SerializeField] private string dungeonSceneName = "Dungeon1Scene";
    [SerializeField] private int targetState = 4;
    [Tooltip("Dungeon'a girilince hangi state'e geçilecek")]
    
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI; // Canvas → DialogBox
    [SerializeField] private Text promptText; // DialogBox içindeki Text component
    [SerializeField] private string enterMessage = "Press E to Enter Dungeon";
    [SerializeField] private float messageDisplayDuration = 2f;
    [Tooltip("Mesaj kaç saniye ekranda kalacak (0 = trigger içinde kalıcı)")]
    
    [Header("Audio")]
    [SerializeField] private AudioClip enterSfx;
    [SerializeField] private AudioSource audioSource;
    
    private bool playerInRange = false;
    private Coroutine hideMessageCoroutine;
    
    private void Start()
    {
        Debug.Log($"[DungeonGate] Initialized: {dungeonID} → {dungeonSceneName} (State {targetState})");
        
        // Prompt gizle
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInRange = true;
        
        Debug.Log($"[DungeonGate] Player entered {dungeonID} trigger");
        
        // Prompt göster
        ShowPrompt();
    }
    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (!playerInRange) return;
        
        // E tuşuna basıldı mı?
        if (Input.GetKeyDown(KeyCode.E))
        {
            EnterDungeon();
        }
    }
    
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        playerInRange = false;
        
        Debug.Log($"[DungeonGate] Player left {dungeonID} trigger");
        
        // Prompt gizle
        HidePrompt();
    }
    
    private void ShowPrompt()
    {
        if (promptUI == null) return;
        
        // Önceki timer varsa iptal et
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
        }
        
        // Text güncelle
        if (promptText != null)
        {
            promptText.text = enterMessage;
        }
        
        // UI göster
        promptUI.SetActive(true);
        
        Debug.Log($"[DungeonGate] Prompt shown: \"{enterMessage}\"");
        
        // Eğer timer varsa (0'dan büyükse), otomatik gizle
        if (messageDisplayDuration > 0)
        {
            hideMessageCoroutine = StartCoroutine(HidePromptAfterDelay(messageDisplayDuration));
        }
    }
    
    private void HidePrompt()
    {
        if (promptUI == null) return;
        
        // Timer varsa iptal et
        if (hideMessageCoroutine != null)
        {
            StopCoroutine(hideMessageCoroutine);
            hideMessageCoroutine = null;
        }
        
        promptUI.SetActive(false);
        Debug.Log($"[DungeonGate] Prompt hidden");
    }
    
    private IEnumerator HidePromptAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        // Sadece player hala trigger içindeyse gizleme
        // (Player trigger'dan çıkmışsa zaten gizlenmiştir)
        if (playerInRange)
        {
            promptUI.SetActive(false);
            Debug.Log($"[DungeonGate] Prompt auto-hidden after {delay}s");
        }
        
        hideMessageCoroutine = null;
    }
    
    private void EnterDungeon()
    {
        Debug.Log($"[DungeonGate] ✅ Entering {dungeonID}!");
        Debug.Log($"[DungeonGate] Loading: {dungeonSceneName} → State {targetState}");
        
        // SFX çal
        if (audioSource != null && enterSfx != null)
        {
            audioSource.PlayOneShot(enterSfx);
        }
        
        // Prompt gizle
        HidePrompt();
        
        // LOADING SCREEN ile dungeon yükle!
        LoadingManager.LoadScene(dungeonSceneName, targetState);
    }
    
    private void OnDrawGizmosSelected()
    {
        // Trigger area görselleştir (yeşil)
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.offset, box.size);
        }
    }
}