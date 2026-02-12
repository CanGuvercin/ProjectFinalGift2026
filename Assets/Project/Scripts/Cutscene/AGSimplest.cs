using UnityEngine;

public class AGSimplest : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 1.5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
    [Header("UI")]
    [SerializeField] private GameObject pressELabel;
    
    // NOT: spawnPoint kaldırıldı! Spawn işini CutsceneChief hallediyor.
    // State'in playerSpawnPosition alanına spawn noktasını Inspector'da ata!
    
    [Header("References")]
    [SerializeField] private CutsceneChief cutsceneChief;

    private Transform player;
    private PlayerController playerController;
    private bool isPlayerNear = false;
    private bool hasBeenActivated = false;
    
    private void Start()
    {
        Debug.Log($"[ActGateSimplest] Initialized at {transform.position}");
        
        if (cutsceneChief == null)
        {
            cutsceneChief = FindObjectOfType<CutsceneChief>();
        }
        
        if (pressELabel != null)
        {
            pressELabel.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (hasBeenActivated) return;
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = player.GetComponent<PlayerController>();
            }
            else
            {
                return;
            }
        }
        
        float distance = Vector2.Distance(transform.position, player.position);
        bool wasNear = isPlayerNear;
        isPlayerNear = distance <= activationRadius;
        
        if (isPlayerNear != wasNear)
        {
            if (pressELabel != null)
            {
                pressELabel.SetActive(isPlayerNear);
                Debug.Log($"[ActGateSimplest] Press E label: {(isPlayerNear ? "SHOWN" : "HIDDEN")}");
            }
        }
        
        if (isPlayerNear && Input.GetKeyDown(interactionKey))
        {
            Activate();
        }
    }
    
    private void Activate()
    {
        if (hasBeenActivated) return;
        hasBeenActivated = true;
        
        Debug.Log("[ActGateSimplest] ========== ACTIVATION ==========");
        
        if (pressELabel != null)
            pressELabel.SetActive(false);
        
        if (playerController != null)
        {
            playerController.FreezePlayer();
            Debug.Log("[ActGateSimplest] Player frozen");
        }
        
        // ✅ Spawn yok! CutsceneChief.AdvanceState() → PlayCurrentState() → SpawnPlayer() halleder
        if (cutsceneChief != null)
        {
            Debug.Log("[ActGateSimplest] Advancing state...");
            cutsceneChief.AdvanceState();
        }
        
        if (playerController != null)
        {
            playerController.UnfreezePlayer();
            Debug.Log("[ActGateSimplest] Player unfrozen");
        }
        
        Debug.Log("[ActGateSimplest] ========== COMPLETE ==========");
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}