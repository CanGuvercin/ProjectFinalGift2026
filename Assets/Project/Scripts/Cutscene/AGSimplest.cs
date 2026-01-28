using UnityEngine;

public class AGSimplest : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 1.5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    
    [Header("UI")]
    [SerializeField] private GameObject pressELabel; // "Press E" sprite child objesi
    
    [Header("Teleport")]
    [SerializeField] private Transform spawnPoint;
    
    [Header("References")]
    [SerializeField] private CutsceneChief cutsceneChief;

    //    
    private Transform player;
    private PlayerController playerController;
    private bool isPlayerNear = false;
    private bool hasBeenActivated = false;
    
    private void Start()
    {
        Debug.Log($"[ActGateSimplest] Initialized at {transform.position}");
        
        // CutsceneChief'i otomatik bul
        if (cutsceneChief == null)
        {
            cutsceneChief = FindObjectOfType<CutsceneChief>();
        }
        
        // Press E label'ı gizle
        if (pressELabel != null)
        {
            pressELabel.SetActive(false);
        }
        
        // Spawn point kontrolü
        if (spawnPoint == null)
        {
            Debug.LogWarning("[ActGateSimplest] No spawn point assigned!");
        }
    }
    
    private void Update()
    {
        if (hasBeenActivated) return;
        
        // Player'ı bul
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
        
        // Mesafe kontrolü
        float distance = Vector2.Distance(transform.position, player.position);
        bool wasNear = isPlayerNear;
        isPlayerNear = distance <= activationRadius;
        
        // Press E label'ı göster/gizle
        if (isPlayerNear != wasNear)
        {
            if (pressELabel != null)
            {
                pressELabel.SetActive(isPlayerNear);
                Debug.Log($"[ActGateSimplest] Press E label: {(isPlayerNear ? "SHOWN" : "HIDDEN")}");
            }
        }
        
        // E tuşuna basıldı mı?
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
        
        // Press E label'ı gizle
        if (pressELabel != null)
        {
            pressELabel.SetActive(false);
        }
        
        // Player'ı dondur
        if (playerController != null)
        {
            playerController.FreezePlayer();
            Debug.Log("[ActGateSimplest] Player frozen");
        }
        
        // Player'ı teleport et
        if (player != null && spawnPoint != null)
        {
            Debug.Log($"[ActGateSimplest] Teleporting to: {spawnPoint.position}");
            player.position = spawnPoint.position;
        }
        
        // State ilerlet
        if (cutsceneChief != null)
        {
            Debug.Log("[ActGateSimplest] Advancing state...");
            cutsceneChief.AdvanceState();
        }
        
        // Player'ı çöz
        if (playerController != null)
        {
            playerController.UnfreezePlayer();
            Debug.Log("[ActGateSimplest] Player unfrozen");
        }
        
        Debug.Log("[ActGateSimplest] ========== COMPLETE ==========");
    }
    
    private void OnDrawGizmosSelected()
    {
        // Activation radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
        
        // Spawn point
        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}