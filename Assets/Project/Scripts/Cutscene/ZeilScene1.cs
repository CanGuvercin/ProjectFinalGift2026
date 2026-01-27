using UnityEngine;
using UnityEngine.Playables;

public class ZeilScene1 : MonoBehaviour
{
    [Header("Cutscene Settings")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool teleportPlayerAfterCutscene = false;
    [SerializeField] private Transform playerSpawnPoint; // Opsiyonel
    
    [Header("State Management")]
    [SerializeField] private CutsceneChief cutsceneChief;
    [SerializeField] private bool advanceStateOnComplete = true;
    
    [Header("Trigger Settings (Optional)")]
    [SerializeField] private bool useAreaTrigger = false;
    [SerializeField] private float triggerRadius = 3f;
    [SerializeField] private Transform triggerCenter;
    [SerializeField] private bool onlyTriggerOnce = true;
    
    private Transform player;
    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private bool hasTriggered = false;
    private bool cutscenePlaying = false;
    
    private void Start()
    {
        // Player'ı bul
        FindPlayer();
        
        // PlayableDirector eventi
        if (playableDirector != null)
        {
            playableDirector.stopped += OnCutsceneComplete;
        }
        
        // Sahne başında otomatik başlat
        if (playOnStart)
        {
            PlayCutscene();
        }
    }
    
    private void Update()
    {
        // Area trigger kontrolü
        if (useAreaTrigger && !hasTriggered && !cutscenePlaying && player != null && triggerCenter != null)
        {
            float distance = Vector3.Distance(player.position, triggerCenter.position);
            if (distance <= triggerRadius)
            {
                if (onlyTriggerOnce)
                    hasTriggered = true;
                
                PlayCutscene();
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
        
        Debug.Log("=== CUTSCENE STARTED ===");
        
        // Player kontrolünü devre dışı bırak
        DisablePlayerControl();
        
        // Cutscene'i oynat
        playableDirector.Play();
    }
    
    private void OnCutsceneComplete(PlayableDirector director)
    {
        if (director != playableDirector) return;
        
        Debug.Log("=== CUTSCENE COMPLETED ===");
        
        cutscenePlaying = false;
        
        // Player'ı spawn noktasına ışınla (eğer gerekiyorsa)
        if (teleportPlayerAfterCutscene && playerSpawnPoint != null && player != null)
        {
            player.position = playerSpawnPoint.position;
            Debug.Log($"Player teleported to spawn point: {playerSpawnPoint.position}");
        }
        
        // Player kontrolünü geri ver
        EnablePlayerControl();
        
        // State'i ilerlet
        if (advanceStateOnComplete && cutsceneChief != null)
        {
            cutsceneChief.AdvanceState();
            Debug.Log("State advanced in CutsceneChief");
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
            playerRb.isKinematic = true; // Fizik etkileşimini durdur
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
        if (useAreaTrigger && triggerCenter != null)
        {
            Gizmos.color = Color.yellow;
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