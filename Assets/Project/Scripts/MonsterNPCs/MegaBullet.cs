using System.Collections;
using UnityEngine;

public class MegaBullet : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private CircleCollider2D bulletCollider;
    
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6.5f;
    [SerializeField] private float boostedSpeedMultiplier = 1.25f;
    [SerializeField] private float lagDelay = 0.35f;
    [SerializeField] private float trackingDuration = 10f;
    
    [Header("Explosion Settings")]
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private float countdownDuration = 2f;
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private int explosionDamage = 1;
    [SerializeField] private float explosionRadius = 1.3f;
    
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 15f;
    
    [Header("Animation")]
    [SerializeField] private float explosionAnimDuration = 0.6f; // Patlama animasyon süresi
    
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
    private bool isBoosted = false;
    private bool hasDealtContactDamage = false;
    
    private float currentMoveSpeed;
    private float lifetimeTimer = 0f;
    private float trackingTimer = 0f;

    void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (bulletCollider == null) bulletCollider = GetComponent<CircleCollider2D>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        currentMoveSpeed = moveSpeed;
    }

    public void Initialize(Transform playerTransform)
    {
        player = playerTransform;
        bossController = FindObjectOfType<ZeilBossController>();
        
        if (player != null)
        {
            laggedTargetPosition = player.position;
        }
        
        StartCoroutine(UpdateLaggedPosition());
    }

    void Update()
    {
        if (hasExploded) return;
        
        if (player == null)
        {
            DestroyBullet();
            return;
        }
        
        // Lifetime check - Max lifetime bitince explode
        lifetimeTimer += Time.deltaTime;
        
        if (lifetimeTimer >= maxLifetime)
        {
            // Hareketi durdur ve patlat
            isTracking = false;
            
            // Eğer henüz yanıp sönme başlamamışsa, başlat
            if (!isBlinking)
            {
                StartBlinking();
            }
            
            return;
        }
        
        // Tracking duration check - 10 saniye sonra takibi bırak (ama patlatma)
        if (isTracking)
        {
            trackingTimer += Time.deltaTime;
            if (trackingTimer >= trackingDuration)
            {
                isTracking = false; // Sadece hareketi durdur
            }
        }
        
        // Hareket
        if (isTracking)
        {
            transform.position = Vector2.MoveTowards(
                transform.position, 
                laggedTargetPosition, 
                currentMoveSpeed * Time.deltaTime
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
        isTracking = false;
        
        // Hız artışı
        if (!isBoosted)
        {
            currentMoveSpeed = moveSpeed * boostedSpeedMultiplier;
            isBoosted = true;
        }
        
        // HEMEN HASAR VER
        DealContactDamage();
        
        // Animator trigger
        animator.SetTrigger("isPlayerNear");
        PlaySound(blinkSound);
        
        // Countdown başlat
        StartCoroutine(CountdownToExplosion());
    }

    void DealContactDamage()
    {
        if (hasDealtContactDamage) return;
        if (player == null) return;
        
        hasDealtContactDamage = true;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRadius)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(contactDamage, transform.position);
            }
        }
    }

    IEnumerator CountdownToExplosion()
    {
        yield return new WaitForSeconds(countdownDuration);
        Explode();
    }

    void Explode()
    {
        if (hasExploded) return;
        
        hasExploded = true;
        
        // Hareketi tamamen durdur
        isTracking = false;
        
        // Animator trigger - Patlama animasyonu
        animator.SetTrigger("Explode");
        PlaySound(explosionSound);
        
        // Physics durdur
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        // Collider kapat (tekrar collision olmasın)
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
        
        // Hasar uygula
        ApplyExplosionDamage();
        
        // Animasyon bitince destroy
        StartCoroutine(DestroyAfterExplosion());
    }

    void ApplyExplosionDamage()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= explosionRadius)
        {
            PlayerController playerController = player.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(explosionDamage, transform.position);
            }
        }
    }

    IEnumerator DestroyAfterExplosion()
    {
        // Patlama animasyonunun bitmesini bekle
        yield return new WaitForSeconds(explosionAnimDuration);
        
        DestroyBullet();
    }

    void DestroyBullet()
    {
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
        
        if (other.CompareTag("Player"))
        {
            // Eğer henüz hasar vermediyse ver
            if (!hasDealtContactDamage)
            {
                DealContactDamage();
            }
            
            StopAllCoroutines();
            Explode();
        }
        
        if (other.CompareTag("Wall") || other.CompareTag("Obstacle") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Detection radius (sarı)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        // Explosion radius (kırmızı)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        // Lifetime indicator (cyan)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.cyan;
            float lifetimeRatio = lifetimeTimer / maxLifetime;
            Gizmos.DrawWireSphere(transform.position, 0.5f + lifetimeRatio);
        }
    }
}