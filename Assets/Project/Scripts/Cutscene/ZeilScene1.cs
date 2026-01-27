using UnityEngine;
using UnityEngine.Playables;

public class ZeilScene1 : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool teleportPlayerAfterCutscene = false;
    [SerializeField] private Transform playerSpawnPoint;
    
    [Header("State Management")]
    [SerializeField] private CutsceneChief cutsceneChief;
    [SerializeField] private bool advanceStateOnCutsceneEnd = false; // ❌ FALSE yap
    
    [Header("Area Trigger for State Advance")]
    [SerializeField] private bool useAreaTriggerForState = true; // ✅ TRUE
    [SerializeField] private float triggerRadius = 2f;
    [SerializeField] private Transform triggerCenter; // Kapının pozisyonu
    [SerializeField] private bool onlyTriggerOnce = true;
    
    private Transform player;
    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private bool hasTriggeredState = false;
    private bool cutscenePlaying = false;
    private bool cutsceneCompleted = false; // Cutscene bitti mi?
    
    private void Start()
    {
        FindPlayer();
        
        if (playableDirector != null)
        {
            playableDirector.stopped += OnCutsceneComplete;
        }
        
        if (playOnStart)
        {
            PlayCutscene();
        }
    }
    
    private void Update()
    {
        // Area trigger - SADECE cutscene bittikten sonra kontrol et
        if (useAreaTriggerForState && 
            cutsceneCompleted && 
            !hasTriggeredState && 
            player != null && 
            triggerCenter != null)
        {
            float distance = Vector3.Distance(player.position, triggerCenter.position);
            
            if (distance <= triggerRadius)
            {
                Debug.Log($"Player entered trigger area! Distance: {distance}");
                
                if (onlyTriggerOnce)
                    hasTriggeredState = true;
                
                AdvanceState();
            }
        }
    }
    
    public void PlayCutscene()
    {
        if (cutscenePlaying) return;
        if (playableDirector == null)
        {
            Debug.LogError("PlayableDirector is not assigned!");
            return;
        }
        
        cutscenePlaying = true;
        cutsceneCompleted = false;
        
        Debug.Log("=== CUTSCENE STARTED ===");
        
        DisablePlayerControl();
        playableDirector.Play();
    }
    
    private void OnCutsceneComplete(PlayableDirector director)
    {
        if (director != playableDirector) return;
        
        Debug.Log("=== CUTSCENE COMPLETED ===");
        
        cutscenePlaying = false;
        cutsceneCompleted = true; // Cutscene bitti flag'i
        
        // Player'ı spawn noktasına ışınla (eğer gerekiyorsa)
        if (teleportPlayerAfterCutscene && playerSpawnPoint != null && player != null)
        {
            player.position = playerSpawnPoint.position;
            Debug.Log($"Player teleported to spawn point: {playerSpawnPoint.position}");
        }
        
        // Player kontrolünü geri ver
        EnablePlayerControl();
        
        // Cutscene bitince hemen state ilerletme (eskiden buradaydı)
        if (advanceStateOnCutsceneEnd && cutsceneChief != null)
        {
            AdvanceState();
        }
        
        Debug.Log("Player control restored. Waiting for area trigger...");
    }
    
    private void AdvanceState()
    {
        if (cutsceneChief != null)
        {
            cutsceneChief.AdvanceState();
            Debug.Log("=== STATE ADVANCED ===");
        }
        else
        {
            Debug.LogWarning("CutsceneChief is not assigned!");
        }
    }
    
    private void DisablePlayerControl()
    {
        if (playerController != null)
        {
            playerController.enabled = false;
            Debug.Log("Player control disabled");
        }
        
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.isKinematic = true;
        }
    }
    
    private void EnablePlayerControl()
    {
        if (playerController != null)
        {
            playerController.enabled = true;
            Debug.Log("Player control enabled");
        }
        
        if (playerRb != null)
        {
            playerRb.isKinematic = false;
        }
    }
    
    private void FindPlayer()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            playerController = playerObj.GetComponent<PlayerController>();
            playerRb = playerObj.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogWarning("Player not found! Make sure player has 'Player' tag.");
        }
    }
    
    private void OnDestroy()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnCutsceneComplete;
        }
    }
    
    // Gizmos - Trigger alanını göster
    private void OnDrawGizmosSelected()
    {
        if (useAreaTriggerForState && triggerCenter != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f); // Turuncu
            Gizmos.DrawSphere(triggerCenter.position, triggerRadius);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(triggerCenter.position, triggerRadius);
        }
        
        if (teleportPlayerAfterCutscene && playerSpawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(playerSpawnPoint.position, 0.5f);
            Gizmos.DrawLine(playerSpawnPoint.position, playerSpawnPoint.position + Vector3.up);
        }
    }
}