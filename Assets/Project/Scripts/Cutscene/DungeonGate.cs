using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DungeonGate : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Text promptText;
    [SerializeField] private string enterMessage = "Press E to Enter Dungeon";
    
    [Header("Audio")]
    [SerializeField] private AudioClip enterSfx;
    [SerializeField] [Range(0f, 2f)] private float soundVolume = 1.0f;
    
    private Transform player;
    private AudioSource playerSFXSource;
    private CutsceneChief cutsceneChief;
    private bool isNearGate = false;
    
    private void Start()
    {
        Debug.Log($"[DungeonGate] ═══════════════════════════════");
        Debug.Log($"[DungeonGate] Simple Gate Initialized!");
        Debug.Log($"[DungeonGate] Position: {transform.position}");
        Debug.Log($"[DungeonGate] Interaction Radius: {interactionRadius}");
        
        // Player'ı bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            
            // PlayerSFX AudioSource'u bul
            Transform sfxChild = playerObj.transform.Find("PlayerSFX");
            if (sfxChild != null)
            {
                playerSFXSource = sfxChild.GetComponent<AudioSource>();
            }
            
            if (playerSFXSource == null)
            {
                Debug.LogWarning("[DungeonGate] PlayerSFX AudioSource not found!");
            }
        }
        else
        {
            Debug.LogError("[DungeonGate] ❌ Player not found!");
        }
        
        // CutsceneChief'i bul
        cutsceneChief = FindObjectOfType<CutsceneChief>();
        if (cutsceneChief == null)
        {
            Debug.LogError("[DungeonGate] ❌ CutsceneChief not found!");
        }
        else
        {
            Debug.Log("[DungeonGate] ✅ CutsceneChief found!");
        }
        
        // Prompt başta kapalı
        if (promptUI != null)
        {
            promptUI.SetActive(false);
            Debug.Log("[DungeonGate] ✅ PromptUI assigned");
        }
        else
        {
            Debug.LogWarning("[DungeonGate] ⚠️ PromptUI is NULL!");
        }
        
        Debug.Log($"[DungeonGate] ═══════════════════════════════");
    }
    
    private void Update()
    {
        if (player == null || cutsceneChief == null) return;
        
        // Player yakınında mı?
        float distance = Vector2.Distance(transform.position, player.position);
        
        // Yakınlık durumu değişti mi?
        bool wasNear = isNearGate;
        isNearGate = distance <= interactionRadius;
        
        if (isNearGate != wasNear)
        {
            if (isNearGate)
            {
                // Player yakına geldi
                Debug.Log($"[DungeonGate] 🚪 Player entered range! Distance: {distance:F2}");
                ShowPrompt();
            }
            else
            {
                // Player uzaklaştı
                Debug.Log($"[DungeonGate] 🚶 Player left range! Distance: {distance:F2}");
                HidePrompt();
            }
        }
        
        // E tuşuna basıldı mı?
        if (isNearGate && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[DungeonGate] 🔑 E KEY PRESSED!");
            EnterDungeon();
        }
    }
    
    private void ShowPrompt()
    {
        if (promptUI == null) return;
        
        if (promptText != null)
        {
            promptText.text = enterMessage;
        }
        
        promptUI.SetActive(true);
        Debug.Log($"[DungeonGate] 💬 Prompt shown: \"{enterMessage}\"");
    }
    
    private void HidePrompt()
    {
        if (promptUI == null) return;
        
        promptUI.SetActive(false);
        Debug.Log($"[DungeonGate] 💬 Prompt hidden");
    }
    
    private void EnterDungeon()
    {
        Debug.Log($"[DungeonGate] ═══════════════════════════════");
        Debug.Log($"[DungeonGate] ✅ ENTERING DUNGEON!");
        
        // SFX çal
        if (playerSFXSource != null && enterSfx != null)
        {
            playerSFXSource.PlayOneShot(enterSfx, soundVolume);
            Debug.Log($"[DungeonGate] 🔊 Playing enter sound (volume: {soundVolume})");
        }
        
        // Prompt gizle
        HidePrompt();
        
        // CutsceneChief'e state ilerlet emri ver!
        Debug.Log($"[DungeonGate] 📢 Telling CutsceneChief to advance state...");
        cutsceneChief.AdvanceState();
        
        Debug.Log($"[DungeonGate] ═══════════════════════════════");
    }
    
    // Debug: Interaction radius göster
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}