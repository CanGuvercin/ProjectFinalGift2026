using UnityEngine;

public class MedicPack : MonoBehaviour
{
    [Header("Healing")]
    [SerializeField] private int healAmount = 30;
    [SerializeField] private bool fullHeal = true;
    
    [Header("Interaction")]
    [SerializeField] private float pickupRadius = 0.8f; // Otomatik pickup mesafesi (eski interactionRadius)
    
    [Header("Visual Feedback")]
    [SerializeField] private GameObject interactPrompt;
    
    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    
    [Header("Floating Animation")]
    [SerializeField] private bool enableFloating = true;
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    
    private Transform player;
    private bool isConsumed = false;
    
    private Vector3 startPosition;
    private float timeOffset;
    
    private void Start()
    {
        // Player'ı bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
        }
        
        // Prompt varsa başta gizle
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(false);
        }
        
        // Floating için başlangıç pozisyonunu kaydet
        startPosition = transform.position;
        
        // Her medic pack farklı fazda başlasın
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    private void Update()
    {
        if (isConsumed || player == null) return;
        
        // Floating animation
        if (enableFloating)
        {
            FloatingAnimation();
        }
        
        // Otomatik pickup kontrolü (YENİ!)
        float distance = Vector2.Distance(transform.position, player.position);
        
        if (distance <= pickupRadius)
        {
            // Otomatik al!
            AutoPickup();
        }
        
        // Prompt göster/gizle (opsiyonel - göstermek istersen)
        if (interactPrompt != null)
        {
            // Biraz daha uzaktan prompt göster (pickup radius'ın 1.5 katı)
            bool showPrompt = distance <= (pickupRadius * 1.5f);
            interactPrompt.SetActive(showPrompt);
        }
    }
    
    private void FloatingAnimation()
    {
        float newY = startPosition.y + Mathf.Sin((Time.time * floatSpeed) + timeOffset) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
    
    private void AutoPickup()
    {
        if (isConsumed) return;
        
        GameObject playerObj = player.gameObject;
        PlayerController playerController = playerObj.GetComponent<PlayerController>();
        
        if (playerController == null)
        {
            Debug.LogWarning("[MedicPack] Player doesn't have PlayerController!");
            return;
        }
        
        // HP'yi heal et
        int healedAmount = fullHeal ? playerController.GetMaxHealth() : healAmount;
        playerController.Heal(healedAmount);
        
        // SFX çal
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Consume et
        isConsumed = true;
        Debug.Log($"[MedicPack] Auto-pickup! Healed: {healedAmount} HP");
        
        // Destroy
        Destroy(gameObject);
    }
    
    // Debug: Radius görselleştir
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pickupRadius);
    }
    
    
    // ================= ESKİ KOD (E TUŞU İLE INTERACT) - YORUMDA =================
    /*
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1f;
    
    private void Update()
    {
        if (isConsumed || player == null) return;
        
        // Floating animation
        if (enableFloating)
        {
            FloatingAnimation();
        }
        
        // Player yakınında mı?
        float distance = Vector2.Distance(transform.position, player.position);
        bool playerInRange = distance <= interactionRadius;
        
        // Prompt göster/gizle
        if (interactPrompt != null)
        {
            interactPrompt.SetActive(playerInRange);
        }
    }
    
    public bool CanInteract(Transform playerTransform)
    {
        if (isConsumed) return false;
        
        float distance = Vector2.Distance(transform.position, playerTransform.position);
        return distance <= interactionRadius;
    }
    
    public void Interact(GameObject playerObject)
    {
        if (isConsumed) return;
        
        PlayerController playerController = playerObject.GetComponent<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[MedicPack] Player doesn't have PlayerController!");
            return;
        }
        
        // HP'yi heal et
        int healedAmount = fullHeal ? playerController.GetMaxHealth() : healAmount;
        playerController.Heal(healedAmount);
        
        // SFX çal
        if (pickupSound != null)
        {
            AudioSource.PlayClipAtPoint(pickupSound, transform.position);
        }
        
        // Consume et
        isConsumed = true;
        Debug.Log($"[MedicPack] Consumed! Healed: {healedAmount} HP");
        
        // Destroy
        Destroy(gameObject);
    }
    */
}