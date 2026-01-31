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
    [SerializeField] private float lagDelay = 0.35f;
    [SerializeField] private float trackingDuration = 10f; // 10 saniye takip et, sonra self-destruct
    
    [Header("Explosion Settings")]
    [SerializeField] private float detectionRadius = 1f;
    [SerializeField] private float countdownDuration = 0.1f;
    [SerializeField] private int explosionDamage = 1;
    [SerializeField] private float explosionRadius = 1.3f;
    
    [Header("Lifetime")]
    [SerializeField] private float maxLifetime = 15f;
    
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
    private float trackingTimer = 0f;

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
        
        // Lifetime check
        lifetimeTimer += Time.deltaTime;
        if (lifetimeTimer >= maxLifetime)
        {
            Explode();
            return;
        }
        
        // Tracking duration check - 10 saniye sonra takibi bırak ve self-destruct
        if (isTracking)
        {
            trackingTimer += Time.deltaTime;
            if (trackingTimer >= trackingDuration)
            {
                isTracking = false;
                StartCoroutine(SelfDestruct());
                return;
            }
        }
        
        // Hareket
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
    }//

    IEnumerator SelfDestruct()
    {
        // Takip sona erdi, 2 saniye bekle ve patlat
        yield return new WaitForSeconds(2f);
        Explode();
    }

    void StartBlinking()
    {
        isBlinking = true;
        isTracking = false;
        
        animator.SetTrigger("isPlayerNear");
        PlaySound(blinkSound);
        
        StartCoroutine(CountdownToExplosion());
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
        
        animator.SetTrigger("Explode");
        PlaySound(explosionSound);
        
        isTracking = false;
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }
        
        if (bulletCollider != null)
        {
            bulletCollider.enabled = false;
        }
        
        ApplyExplosionDamage();
        
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
        yield return new WaitForSeconds(0.6f);
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
            StopAllCoroutines();
            Explode();
        }
        
        if (other.CompareTag("Wall") || other.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Explode();
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}