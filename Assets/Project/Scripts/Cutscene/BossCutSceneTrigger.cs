using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

public class BossCutsceneTrigger : MonoBehaviour
{
    [Header("Trigger Settings")]
    [SerializeField] private BoxCollider2D triggerArea;
    [SerializeField] private float checkRadius = 2f; // Manuel kontrol için
    
    [Header("Cutscene")]
    [SerializeField] private PlayableDirector cutsceneDirector;
    
    [Header("Camera Settings")]
    [SerializeField] private GameObject mainCamera; // Ana kamera (kapatılacak)
    [SerializeField] private GameObject cutsceneCamera; // Cutscene kamerası (açılacak)
    
    [Header("State Management")]
    [SerializeField] private CutsceneChief cutsceneChief;
    
    [Header("Player References")]
    private Transform player;
    private PlayerController playerController;
    private Animator playerAnimator;
    private Rigidbody2D playerRb;
    
    private bool hasTriggered = false;
    private bool cutsceneFinished = false;

    private void Awake()
    {
        Debug.Log("[BossCutsceneTrigger] ========== AWAKE ==========");
        Debug.Log($"[BossCutsceneTrigger] GameObject: {gameObject.name}");
        Debug.Log($"[BossCutsceneTrigger] Position: {transform.position}");
    }

    private void Start()
    {
        Debug.Log("[BossCutsceneTrigger] ========== START ==========");
        
        // Trigger area setup
        if (triggerArea == null)
        {
            triggerArea = GetComponent<BoxCollider2D>();
            if (triggerArea == null)
            {
                triggerArea = gameObject.AddComponent<BoxCollider2D>();
                Debug.Log("[BossCutsceneTrigger] ⚠️ Created new BoxCollider2D!");
            }
        }
        
        triggerArea.isTrigger = true;
        
        // BU SCRIPTIN OLDUĞU OBJEYE RİGİDBODY2D EKLE
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static; // Hareket etmeyecek
            rb.gravityScale = 0f;
            Debug.Log("[BossCutsceneTrigger] ✅ Added Rigidbody2D (Static)");
        }
        
        Debug.Log($"[BossCutsceneTrigger] Trigger Area: {triggerArea.bounds.size}, isTrigger: {triggerArea.isTrigger}");
        
        // Kamera kontrolü
        if (mainCamera != null)
        {
            Debug.Log($"[BossCutsceneTrigger] ✅ Main Camera assigned: {mainCamera.name}");
        }
        else
        {
            Debug.LogWarning("[BossCutsceneTrigger] ⚠️ Main Camera NOT ASSIGNED!");
        }
        
        if (cutsceneCamera != null)
        {
            Debug.Log($"[BossCutsceneTrigger] ✅ Cutscene Camera assigned: {cutsceneCamera.name}");
            // Cutscene kamerasını başta kapat
            cutsceneCamera.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[BossCutsceneTrigger] ⚠️ Cutscene Camera NOT ASSIGNED!");
        }
        
        // CutsceneChief'i bul
        if (cutsceneChief == null)
        {
            cutsceneChief = FindObjectOfType<CutsceneChief>();
            if (cutsceneChief != null)
            {
                Debug.Log("[BossCutsceneTrigger] ✅ CutsceneChief found automatically");
            }
            else
            {
                Debug.LogWarning("[BossCutsceneTrigger] ⚠️ CutsceneChief NOT FOUND!");
            }
        }
        
        // PlayableDirector kontrolü ve event listener
        if (cutsceneDirector != null)
        {
            Debug.Log($"[BossCutsceneTrigger] ✅ PlayableDirector assigned: {cutsceneDirector.name}");
            
            // Timeline bittiğinde tetiklenecek event
            cutsceneDirector.stopped += OnCutsceneFinished;
            Debug.Log("[BossCutsceneTrigger] ✅ Stopped event listener added");
        }
        else
        {
            Debug.LogWarning("[BossCutsceneTrigger] ⚠️ PlayableDirector NOT ASSIGNED!");
        }
        
        Debug.Log("[BossCutsceneTrigger] Initialized and waiting for player...");
    }

    private void OnDestroy()
    {
        // Event listener'ı temizle
        if (cutsceneDirector != null)
        {
            cutsceneDirector.stopped -= OnCutsceneFinished;
            Debug.Log("[BossCutsceneTrigger] Event listener removed");
        }
    }

    private void Update()
    {
        // Manuel mesafe kontrolü (fallback)
        if (hasTriggered) return;
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        if (player != null)
        {
            float distance = Vector2.Distance(transform.position, player.position);
            if (distance <= checkRadius)
            {
                Debug.Log($"[BossCutsceneTrigger] 🔍 MANUAL DISTANCE CHECK: Player within {checkRadius}m!");
                TriggerCutscene();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[BossCutsceneTrigger] 🔍 OnTriggerEnter2D called! Object: {other.gameObject.name}, Tag: {other.tag}");
        
        if (hasTriggered)
        {
            Debug.Log("[BossCutsceneTrigger] ❌ Already triggered, ignoring...");
            return;
        }
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("[BossCutsceneTrigger] ✅✅✅ PLAYER DETECTED VIA TRIGGER! ✅✅✅");
            TriggerCutscene();
        }
        else
        {
            Debug.Log($"[BossCutsceneTrigger] ❌ Not player. Tag was: '{other.tag}'");
        }
    }

    private void TriggerCutscene()
    {
        if (hasTriggered) return;
        
        hasTriggered = true;
        
        // Player referanslarını al
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }
        
        if (player != null)
        {
            playerController = player.GetComponent<PlayerController>();
            playerAnimator = player.GetComponent<Animator>();
            playerRb = player.GetComponent<Rigidbody2D>();
            
            Debug.Log($"[BossCutsceneTrigger] PlayerController: {(playerController != null ? "FOUND" : "NULL")}");
            Debug.Log($"[BossCutsceneTrigger] Animator: {(playerAnimator != null ? "FOUND" : "NULL")}");
            Debug.Log($"[BossCutsceneTrigger] Rigidbody2D: {(playerRb != null ? "FOUND" : "NULL")}");
        }
        
        Debug.Log("[BossCutsceneTrigger] 🎬 Starting boss cutscene...");
        
        StartCoroutine(StartBossCutscene());
    }

    private IEnumerator StartBossCutscene()
    {
        Debug.Log("[BossCutsceneTrigger] ========== CUTSCENE SEQUENCE START ==========");
        
        // 0. KAMERA DEĞİŞTİR - ANA KAMERAYI KAPAT, CUTSCENE KAMERASINI AÇ
        if (mainCamera != null)
        {
            mainCamera.SetActive(false);
            Debug.Log("[BossCutsceneTrigger] 📷 Main Camera DISABLED");
        }
        
        if (cutsceneCamera != null)
        {
            cutsceneCamera.SetActive(true);
            Debug.Log("[BossCutsceneTrigger] 🎥 Cutscene Camera ENABLED");
        }
        
        // 1. INPUT'U KES VE PLAYER'I DURDUR
        if (playerController != null)
        {
            playerController.FreezePlayer();
            Debug.Log("[BossCutsceneTrigger] ✅ Player frozen (input cut)");
        }
        
        // 2. Fiziksel hareketi ANINDA durdur
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            Debug.Log("[BossCutsceneTrigger] ✅ Rigidbody stopped");
        }
        
        // 3. TÜM ANIMATOR PARAMETRELERİNİ SIFIRLA
        if (playerAnimator != null)
        {
            // Tüm parametreleri sıfırla
            playerAnimator.SetFloat("Horizontal", 0f);
            playerAnimator.SetFloat("Vertical", 0f);
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("isRunning", false);
            playerAnimator.SetBool("isAttacking", false);
            
            // Tüm trigger'ları reset et
            playerAnimator.ResetTrigger("Attack");
            
            Debug.Log("[BossCutsceneTrigger] ✅ All animator parameters reset");
        }
        
        // Bir frame bekle
        yield return null;
        
        // 4. ANIMATOR'Ü TAMAMEN SIFIRLA VE DEFAULT STATE'E ZORLA
        if (playerAnimator != null)
        {
            // Animator'ü rebind et (tüm state'leri sıfırlar)
            playerAnimator.Rebind();
            Debug.Log("[BossCutsceneTrigger] ✅ Animator rebinded (full reset)");
            
            // Bir frame daha bekle
            yield return null;
            
            // Şimdi idle state'e geç
            playerAnimator.Play("_Idle_Down_Ali", 0, 0f);
            Debug.Log("[BossCutsceneTrigger] ✅ Forced to _Idle_Down_Ali after rebind");
        }
        
        // Animasyon geçişinin tamamlanması için bekle
        yield return new WaitForSeconds(0.1f);
        
        // 5. Cutscene'i başlat
        if (cutsceneDirector != null)
        {
            Debug.Log("[BossCutsceneTrigger] 🎬 Playing cutscene...");
            cutsceneDirector.Play();
            
            // Event-based bekleme (daha güvenilir)
            Debug.Log("[BossCutsceneTrigger] ⏳ Waiting for cutscene to finish via event...");
            
            // Cutscene bitene kadar bekle
            while (!cutsceneFinished)
            {
                yield return null;
            }
            
            Debug.Log("[BossCutsceneTrigger] ✅ Cutscene finished via event!");
        }
        else
        {
            Debug.LogError("[BossCutsceneTrigger] ❌ PlayableDirector is NULL!");
        }
        
        // Kısa bir güvenlik beklemesi
        yield return new WaitForSeconds(0.2f);
        
        // 6. State'i ilerlet (Boss fight sahnesine geçiş)
        if (cutsceneChief != null)
        {
            Debug.Log("[BossCutsceneTrigger] ⏩ Advancing to next state (Boss Fight)...");
            cutsceneChief.AdvanceState();
            Debug.Log("[BossCutsceneTrigger] ✅ State advanced - Boss fight loading!");
        }
        else
        {
            Debug.LogError("[BossCutsceneTrigger] ❌ CutsceneChief is NULL!");
            
            // Fallback: Player'ı çöz
            if (playerController != null)
            {
                playerController.UnfreezePlayer();
                Debug.Log("[BossCutsceneTrigger] ✅ Player unfrozen (fallback)");
            }
        }
        
        Debug.Log("[BossCutsceneTrigger] ========== CUTSCENE SEQUENCE END ==========");
    }

    // Timeline bittiğinde otomatik tetiklenir
    private void OnCutsceneFinished(PlayableDirector director)
    {
        Debug.Log("[BossCutsceneTrigger] 🎉 OnCutsceneFinished EVENT TRIGGERED!");
        cutsceneFinished = true;
    }

    private void OnDrawGizmosSelected()
    {
        // Trigger area'yı çiz
        Gizmos.color = new Color(1f, 0f, 1f, 0.3f); // Magenta
        if (triggerArea != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(triggerArea.offset, triggerArea.size);
        }
        
        // Manuel check radius'u göster
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, checkRadius);
    }
}