using System.Collections;
using UnityEngine;

public class MegaBullet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CircleCollider2D bulletCollider;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float lagDelay = 0.3f; // Lagged tracking
    
    [Header("Explosion Settings")]
    [SerializeField] private float detectionRadius = 2f; // Player bu radius'a girince yanıp sönme başlar
    [SerializeField] private float countdownDuration = 2f; // Yanıp sönme süresi
    [SerializeField] private int explosionDamage = 1; // int olmalı (PlayerController int alıyor)
    [SerializeField] private float explosionRadius = 1.5f; // Patlama hasar radius'u
    
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 15f; // Eğer hiç patlamazsa self-destruct
    
    [Header("Audio")]
    [SerializeField] private AudioClip blinkSound;
    [SerializeField] private AudioClip explosionSound;
    [SerializeField] private AudioSource audioSource;
    
    private Transform player;
    private Vector2 laggedTargetPosition;
    private ZeilBossController bossController;
    
    private bool isTracking = true;
    private bool isBlinking = false;
    private bool hasExploded = false;
    
    private float lifetimeTimer = 0f;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bulletCollider == null) bulletCollider = GetComponent<CircleCollider2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
    }

    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        bossController = FindObjectOfType<ZeilBossController>();
        
        // Lagged tracking başlat
        StartCoroutine(UpdateLaggedPosition());
    }

    void Update()
    {
        if (hasExploded) return;
        
        // Player yoksa destroy
        if (player == null)
        {
            DestroyBullet();
            return;
        }
        
        // Lifetime kontrolü
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            Explode();
            return;
        }
        
        // Hareket (lagged position'a doğru)
        if (isTracking)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, 
                laggedTargetPosition, 
                moveSpeed * Time.deltaTime
            );
        }
        
        // Player proximity check
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRadius && !isBlinking)
        {
            StartBlinking();
        }
    }

    IEnumerator UpdateLaggedPosition()
    {
        while (isTracking && player != null)
        {
            laggedTargetPosition = player.position;
            yield return new WaitForSeconds(lagDelay);
        }
    }

    void StartBlinking()
    {
        isBlinking = true;
        isTracking = false; // Yanıp sönerken hareket etme
        
        // Animator'a trigger gönder
        animator.SetTrigger("isPlayerNear");
        
        // Ses efekti
        PlaySound(blinkSound);
        
        // Countdown başlat
        StartCoroutine(CountdownToExplosion());
    }

    IEnumerator CountdownToExplosion()
    {
        yield return new WaitForSeconds(countdownDuration);
        
        // Countdown bitti, patlama!
        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        
        // Animator'a trigger gönder
        animator.SetTrigger("Explode");
        
        // Ses efekti
        PlaySound(explosionSound);
        
        // Hareket durdur
        isTracking = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // Collider'ı kapat (tekrar collision olmasın)
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
        
        // Explosion damage kontrolü
        ApplyExplosionDamage();
        
        // Animasyon bitince destroy
        StartCoroutine(DestroyAfterExplosion());
    }

    void ApplyExplosionDamage()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        // Player explosion radius içindeyse hasar ver
        if (distanceToPlayer <= explosionRadius)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // TakeDamage(int damage, Vector2 damageSourcePos) çağrısı
                playerController.TakeDamage(explosionDamage, transform.position);
            }
        }
    }

    IEnumerator DestroyAfterExplosion()
    {
        // BulletExplosion animasyon süresini bekle
        yield return new WaitForSeconds(0.5f); // Animasyon süresine göre ayarla
        
        DestroyBullet();
    }

    void DestroyBullet()
    {
        // Boss controller'a bildir
        if (bossController != null)
        {
            bossController.OnMegaBulletDestroyed();
        }
        
        Destroy(gameObject);
    }

    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;
        
        // Player'a direkt temas
        if (other.CompareTag("Player"))
        {
            // Yanıp sönme safhasında olsa bile direkt patlat
            StopAllCoroutines(); // Countdown'u durdur
            Explode();
        }
        
        // Duvara çarparsa da patlat
        if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    // Debug için Gizmos
    void OnDrawGizmosSelected()
    {
        // Detection radius (sarı)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Explosion radius (kırmızı)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}