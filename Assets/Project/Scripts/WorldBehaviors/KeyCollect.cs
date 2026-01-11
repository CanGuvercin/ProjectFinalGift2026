using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class KeyCollect : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Text promptText;
    [SerializeField] private string collectMessage = "Press E to Collect Key";
    
    [Header("Key Animation Settings")]
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float collectRiseDuration = 0.5f;
    [SerializeField] private float collectRiseHeight = 1.5f;
    [SerializeField] private float collectSpinSpeed = 360f; // Başlangıç dönüş hızı (derece/saniye)
    [SerializeField] private float collectSpinAcceleration = 720f; // Hızlanma (derece/saniye²)
    [SerializeField] private float collectScaleMultiplier = 1.5f; // Maksimum scale
    [SerializeField] private float collectShrinkDuration = 0.8f; // Küçülme süresi
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] [Range(0f, 2f)] private float collectSoundVolume = 1.0f;
    [Tooltip("Anahtar toplandığında çalacak ses")]
    [SerializeField] private AudioClip victorySound;
    [SerializeField] [Range(0f, 2f)] private float victorySoundVolume = 1.0f;
    [Tooltip("Ali zafer pozu sırasında çalacak ses")]
    
    [Header("Victory Animation")]
    [SerializeField] private string victoryTriggerName = "isVictory";
    [Tooltip("Ali'nin Animator'ındaki victory trigger adı")]
    [SerializeField] private float victoryAnimationDuration = 2.0f;
    [Tooltip("Victory animasyonunun tahmini süresi (saniye)")]
    [SerializeField] private float postVictoryDelay = 0.5f;
    [Tooltip("Zafer pozu bitince bekleme süresi")]
    
    [Header("Scene Transition")]
    [SerializeField] private string returnSceneName = "WorldMap";
    [SerializeField] private int nextState = 5;
    [SerializeField] private string returnSpawnPoint = "";
    [Tooltip("WorldMap'te spawn noktası (boş bırakılırsa state'in spawn'ı kullanılır)")]
    
    private Transform player;
    private AudioSource playerSFXSource;
    private bool isCollected = false;
    private bool isNearKey = false;
    
    private Vector3 startPosition;
    private float timeOffset;
    private SpriteRenderer keySprite;
    
    private void Start()
    {
        Debug.Log($"[KeyCollect] ═══════════════════════════════");
        Debug.Log($"[KeyCollect] Key Initialized!");
        Debug.Log($"[KeyCollect] Position: {transform.position}");
        Debug.Log($"[KeyCollect] Next State: {nextState}");
        
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
        }
        else
        {
            Debug.LogError("[KeyCollect] ❌ Player not found!");
        }
        
        // Sprite renderer'ı al
        keySprite = GetComponent<SpriteRenderer>();
        if (keySprite == null)
        {
            Debug.LogWarning("[KeyCollect] ⚠️ SpriteRenderer not found!");
        }
        
        // Prompt başta kapalı
        if (promptUI != null)
        {
            promptUI.SetActive(false);
            Debug.Log("[KeyCollect] ✅ PromptUI assigned");
        }
        else
        {
            Debug.LogWarning("[KeyCollect] ⚠️ PromptUI is NULL!");
        }
        
        // Floating için başlangıç pozisyonunu kaydet
        startPosition = transform.position;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
        
        Debug.Log($"[KeyCollect] ═══════════════════════════════");
    }
    
    private void Update()
    {
        if (isCollected || player == null) return;
        
        // Idle floating animation
        IdleFloating();
        
        // Player yakınında mı?
        float distance = Vector2.Distance(transform.position, player.position);
        
        // Yakınlık durumu değişti mi?
        bool wasNear = isNearKey;
        isNearKey = distance <= interactionRadius;
        
        if (isNearKey != wasNear)
        {
            if (isNearKey)
            {
                Debug.Log($"[KeyCollect] 🔑 Player in range! Distance: {distance:F2}");
                ShowPrompt();
            }
            else
            {
                Debug.Log($"[KeyCollect] 🚶 Player left range! Distance: {distance:F2}");
                HidePrompt();
            }
        }
        
        // E tuşuna basıldı mı?
        if (isNearKey && Input.GetKeyDown(interactKey))
        {
            Debug.Log($"[KeyCollect] 🔑 E KEY PRESSED! Collecting key...");
            StartCoroutine(CollectSequence());
        }
    }
    
    private void IdleFloating()
    {
        // MedicPack'deki gibi yukarı aşağı hafif sallanma
        float newY = startPosition.y + Mathf.Sin((Time.time * floatSpeed) + timeOffset) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
    
    private void ShowPrompt()
    {
        if (promptUI == null) return;
        
        if (promptText != null)
        {
            promptText.text = collectMessage;
        }
        
        promptUI.SetActive(true);
        Debug.Log($"[KeyCollect] 💬 Prompt shown: \"{collectMessage}\"");
    }
    
    private void HidePrompt()
    {
        if (promptUI == null) return;
        
        promptUI.SetActive(false);
        Debug.Log($"[KeyCollect] 💬 Prompt hidden");
    }
    
    private IEnumerator CollectSequence()
    {
        isCollected = true;
        HidePrompt();
        
        Debug.Log($"[KeyCollect] ═══════════════════════════════");
        Debug.Log($"[KeyCollect] ✨ COLLECT SEQUENCE STARTED!");
        
        // SFX çal
        if (playerSFXSource != null && collectSound != null)
        {
            playerSFXSource.PlayOneShot(collectSound, collectSoundVolume);
            Debug.Log($"[KeyCollect] 🔊 Playing collect sound");
        }
        
        // SEQUENCE 1: Key Animation
        yield return StartCoroutine(KeyCollectAnimation());
        
        // SEQUENCE 2: Ali Victory Pose
        yield return StartCoroutine(AliVictoryAnimation());
        
        // SEQUENCE 3: Scene Transition
        Debug.Log($"[KeyCollect] 🌍 Loading {returnSceneName} → State {nextState}");
        LoadingManager.LoadScene(returnSceneName, nextState, returnSpawnPoint);
        
        Debug.Log($"[KeyCollect] ═══════════════════════════════");
    }
    
    private IEnumerator KeyCollectAnimation()
    {
        Debug.Log($"[KeyCollect] 🔑 Phase 1: Key Animation");
        
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * collectRiseHeight;
        Vector3 originalScale = transform.localScale;
        Vector3 maxScale = originalScale * collectScaleMultiplier;
        
        float currentSpinSpeed = collectSpinSpeed;
        float elapsed = 0f;
        
        // PHASE 1: Yüksel + Scale büyüt + Dön
        while (elapsed < collectRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectRiseDuration;
            
            // Yukarı yüksel
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            // Scale büyüt (ease-out)
            float scaleT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(originalScale, maxScale, scaleT);
            
            // Dönüş (giderek hızlanan)
            currentSpinSpeed += collectSpinAcceleration * Time.deltaTime;
            transform.Rotate(Vector3.forward, currentSpinSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        Debug.Log($"[KeyCollect] 🔑 Rise complete! Now shrinking...");
        
        // PHASE 2: Küçül ve kaybol (hızla dönmeye devam et)
        elapsed = 0f;
        Vector3 finalPos = transform.position;
        
        while (elapsed < collectShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectShrinkDuration;
            
            // Küçül (ease-in)
            float shrinkT = 1f - t;
            transform.localScale = maxScale * shrinkT;
            
            // Çok hızlı dön
            currentSpinSpeed += collectSpinAcceleration * Time.deltaTime;
            transform.Rotate(Vector3.forward, currentSpinSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        // Anahtar sprite'ını yok et
        if (keySprite != null)
        {
            keySprite.enabled = false;
        }
        
        Debug.Log($"[KeyCollect] 🔑 Key animation complete! Key destroyed.");
    }
    
    private IEnumerator AliVictoryAnimation()
    {
        Debug.Log($"[KeyCollect] 🎉 Phase 2: Ali Victory Pose");
        
        if (player == null)
        {
            Debug.LogError("[KeyCollect] ❌ Player is null! Skipping victory animation.");
            yield break;
        }
        
        // Ali'nin Animator'ını bul
        Animator aliAnimator = player.GetComponent<Animator>();
        if (aliAnimator == null)
        {
            Debug.LogError("[KeyCollect] ❌ Ali Animator not found!");
            yield break;
        }
        
        // Victory trigger'ını gönder
        aliAnimator.SetTrigger(victoryTriggerName);
        Debug.Log($"[KeyCollect] 🎬 Victory trigger sent: {victoryTriggerName}");
        
        // Victory SFX çal
        if (playerSFXSource != null && victorySound != null)
        {
            playerSFXSource.PlayOneShot(victorySound, victorySoundVolume);
            Debug.Log($"[KeyCollect] 🔊 Playing victory sound");
        }
        
        // Animasyon süresini bekle
        Debug.Log($"[KeyCollect] ⏱️ Waiting for animation ({victoryAnimationDuration:F2}s) + {postVictoryDelay}s");
        yield return new WaitForSeconds(victoryAnimationDuration);
        
        // Post-victory delay
        yield return new WaitForSeconds(postVictoryDelay);
        
        Debug.Log($"[KeyCollect] 🎉 Victory sequence complete!");
    }
    
    // Debug: Radius görselleştir
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}