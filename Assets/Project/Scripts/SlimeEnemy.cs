using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SlimeEnemy : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform shootPoint;
    
    [Header("SFX")]
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioClip shootSfx;
    [SerializeField] private AudioClip chargeSfx;
    [SerializeField] private AudioClip dieSfx;
    
    [Header("Combat Stats")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private int currentHealth;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;
    
    [Header("Range Settings")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float shootingRange = 6f;
    [SerializeField] private float chargeRange = 3f;
    [SerializeField] private float chaseSpeed = 1.5f;
    [SerializeField] private float chargeSpeed = 4f;
    
    [Header("Shooting Settings")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private int burstCount = 2;
    [SerializeField] private float burstDelay = 0.3f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float projectileSpeed = 5f;
    
    [Header("Patrol Settings")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private float patrolSpeed = 0.8f;
    [SerializeField] private float patrolDistance = 3f;
    [SerializeField] private float patrolWaitTime = 1.5f;
    
    [Header("Collision Detection")]
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Invulnerability")]
    [SerializeField] private float hitInvulnerableTime = 0.5f;
    
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private CircleCollider2D circleCollider;
    
    private float lastShootTime;
    private bool isShooting;
    private bool isCharging;
    private bool isInvulnerable;
    private bool isKnockedBack;
    private bool isDead;
    
    // Patrol variables
    private Vector2 patrolStartPos;
    private Vector2 patrolTargetPos;
    private bool isPatrolling;
    private bool isWaitingAtPatrolPoint;
    
    private enum SlimeState { Idle, Patrolling, Shooting, Charging }
    private SlimeState currentState = SlimeState.Idle;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        audioSource = GetComponent<AudioSource>();
        
        currentHealth = maxHealth;
        
        // Rigidbody ayarları
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        // Patrol başlangıç pozisyonu
        patrolStartPos = transform.position;
        SetNewPatrolTarget();
    }
    
    private void Start()
    {
        // Player'ı otomatik bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }
    
    private void Update()
    {
        if (isDead || isKnockedBack) return;
        
        UpdateBehavior();
    }
    
    private void FixedUpdate()
    {
        if (isDead || isKnockedBack) return;
        
        HandleMovement();
    }
    
    // ================= BEHAVIOR =================
    
    private void UpdateBehavior()
    {
        if (player == null) return;
        
        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool hasLineOfSight = HasLineOfSight();
        
        // Player detection range içinde mi?
        bool playerDetected = distanceToPlayer <= detectionRange && hasLineOfSight;
        
        if (playerDetected)
        {
            // Combat states
            if (distanceToPlayer <= chargeRange)
            {
                ChangeState(SlimeState.Charging);
            }
            else if (distanceToPlayer <= shootingRange)
            {
                ChangeState(SlimeState.Shooting);
            }
            else
            {
                // Player görünüyor ama menzil dışında, idle bekle
                ChangeState(SlimeState.Idle);
            }
        }
        else
        {
            // Player yok, patrol yap
            if (enablePatrol && !isWaitingAtPatrolPoint)
            {
                ChangeState(SlimeState.Patrolling);
            }
            else
            {
                ChangeState(SlimeState.Idle);
            }
        }
        
        // State'e göre davranış
        switch (currentState)
        {
            case SlimeState.Shooting:
                TryShoot();
                break;
                
            case SlimeState.Charging:
                if (!isCharging)
                {
                    StartCoroutine(ChargeRoutine());
                }
                break;
                
            case SlimeState.Patrolling:
                HandlePatrol();
                break;
        }
    }
    
    private void ChangeState(SlimeState newState)
    {
        if (currentState == newState) return;

        Debug.Log($"[SLIME] State changed: {currentState} → {newState}");
        
        currentState = newState;
        
        // Animasyon trigger'ları
        switch (newState)
        {
        case SlimeState.Idle:
            rb.velocity = Vector2.zero;
            Debug.Log("[SLIME] → Idle state");
            break;
            
        case SlimeState.Patrolling:
            isPatrolling = true;
            Debug.Log("[SLIME] → Patrolling");
            break;
            
        case SlimeState.Shooting:
            rb.velocity = Vector2.zero;
            isPatrolling = false;
            Debug.Log("[SLIME] → Shooting mode");
            break;
            
        case SlimeState.Charging:
            isPatrolling = false;
            Debug.Log("[SLIME] → CHARGE TRIGGER!");
            animator.SetTrigger("Charge");
            PlaySfx(chargeSfx);
            break;
    }
    }
    
    // ================= PATROL =================
    
    private void SetNewPatrolTarget()
    {
        // Random yön seç (sol veya sağ)
        float randomDir = Random.Range(0f, 1f) > 0.5f ? 1f : -1f;
        patrolTargetPos = patrolStartPos + new Vector2(randomDir * patrolDistance, 0);
    }
    
    private void HandlePatrol()
    {
        if (isWaitingAtPatrolPoint) return;
        
        float distanceToTarget = Vector2.Distance(transform.position, patrolTargetPos);
        
        if (distanceToTarget < 0.2f)
        {
            // Hedefe ulaştı, bekle
            StartCoroutine(WaitAtPatrolPoint());
        }
    }
    
    private IEnumerator WaitAtPatrolPoint()
    {
        isWaitingAtPatrolPoint = true;
        rb.velocity = Vector2.zero;
        
        yield return new WaitForSeconds(patrolWaitTime);
        
        // Yeni hedef belirle
        SetNewPatrolTarget();
        isWaitingAtPatrolPoint = false;
    }
    
    // ================= LINE OF SIGHT =================
    
    private bool HasLineOfSight()
    {
        if (player == null) return false;
        
        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);
        
        // Player'a raycast at, araya obstacle giriyor mu kontrol et
        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);
        
        // Eğer bir obstacle hit olduysa, player görünmüyor
        return hit.collider == null;
    }
    
    // ================= MOVEMENT =================
    
    private void HandleMovement()
    {
        if (isShooting || isCharging || currentState == SlimeState.Idle)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        if (currentState == SlimeState.Patrolling)
        {
            Vector2 direction = (patrolTargetPos - (Vector2)transform.position).normalized;
            rb.velocity = direction * patrolSpeed;
            return;
        }
        
        // Shooting range'de iken yavaşça yaklaş
        if (currentState == SlimeState.Shooting && !isShooting)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * chaseSpeed;
        }
    }
    
    // ================= SHOOTING =================
    
    private void TryShoot()
    {
        if (isShooting) return;
        if (Time.time - lastShootTime < shootCooldown) return;
        
        StartCoroutine(ShootBurstRoutine());
    }
    
   private IEnumerator ShootBurstRoutine()
{
    isShooting = true;
    lastShootTime = Time.time;
    
    for (int i = 0; i < burstCount; i++)
    {
        Debug.Log("[SLIME] → SHOOT TRIGGER!");
        animator.SetTrigger("Shoot");
        yield return new WaitForSeconds(0.1f);
        
        ShootProjectile();
        PlaySfx(shootSfx);
        
        if (i < burstCount - 1)
            yield return new WaitForSeconds(burstDelay);
    }
    
    yield return new WaitForSeconds(0.3f);
    isShooting = false;
}
    
    private void ShootProjectile()
{
    if (projectilePrefab == null)
    {
        Debug.LogError("[SLIME] Projectile Prefab is NULL!");
        return;
    }
    
    if (player == null)
    {
        Debug.LogError("[SLIME] Player is NULL!");
        return;
    }
    
    Vector2 direction = (player.position - transform.position).normalized;
    
    Transform spawnPoint = shootPoint != null ? shootPoint : transform;
    
    Debug.Log($"[SLIME] Spawning projectile at {spawnPoint.position}");
    
    GameObject projectile = Instantiate(projectilePrefab,
    transform.position + (Vector3)direction * 0.5f,
    Quaternion.identity);

    
    Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
    if (projRb != null)
    {
        projRb.velocity = direction * projectileSpeed;
        Debug.Log($"[SLIME] Projectile velocity: {projRb.velocity}");
    }
    else
    {
        Debug.LogError("[SLIME] Projectile has no Rigidbody2D!");
    }
    
    float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
}
    
    // ================= CHARGING =================
    
    private IEnumerator ChargeRoutine()
    {
        isCharging = true;
        
        Vector2 chargeDirection = (player.position - transform.position).normalized;
        
        float chargeDuration = 0.8f;
        float elapsed = 0f;
        
        while (elapsed < chargeDuration)
        {
            rb.velocity = chargeDirection * chargeSpeed;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        rb.velocity = Vector2.zero;
        
        yield return new WaitForSeconds(0.5f);
        isCharging = false;
    }
    
    // ================= DAMAGE SYSTEM =================
    
    public void TakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (isInvulnerable || isDead) return;
        
        currentHealth -= damage;
        animator.SetTrigger("Hit");
        PlaySfx(hitSfx);
        
        // Knockback yönü: hasar kaynağından UZAKLAŞ
        Vector2 knockbackDir = ((Vector2)transform.position - damageSourcePos).normalized;
        StartCoroutine(KnockbackRoutine(knockbackDir));
        
        if (currentHealth <= 0)
        {
            Die();
        }
        else
        {
            StartCoroutine(InvulnerabilityRoutine());
        }
    }
    
    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;
        
        // Hallaç pamuğu gibi savrulma!
        rb.velocity = direction * knockbackForce;
        
        yield return new WaitForSeconds(knockbackDuration);
        
        rb.velocity = Vector2.zero;
        isKnockedBack = false;
    }
    
    private IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(hitInvulnerableTime);
        isInvulnerable = false;
    }
    
    private void Die()
    {
        if (isDead) return;
        
        isDead = true;
        animator.SetTrigger("Die");
        PlaySfx(dieSfx);
        
        rb.velocity = Vector2.zero;
        circleCollider.enabled = false;
        
        Destroy(gameObject, 1f);
    }
    
    // ================= COLLISION =================
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;
        
        // Player'a temas
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController playerController = collision.gameObject.GetComponent<PlayerController>();
            if (playerController != null)
            {
                // Player'a hasar ver
                playerController.TakeDamage(contactDamage, transform.position);
                
                // Slime de temastan geriye savrulsun
                Vector2 knockbackDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
                StartCoroutine(KnockbackRoutine(knockbackDir));
            }
        }
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;
        
        // Player'ın attack'ından hasar al
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerAttack"))
        {
            // Sword'un pozisyonundan knockback hesapla
            Vector2 swordPos = other.transform.position;
            TakeDamage(10, swordPos);
        }
    }
    
    // ================= AUDIO =================
    
    private void PlaySfx(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip);
        }
    }
    
    // ================= DEBUG =================
    
    private void OnDrawGizmosSelected()
    {
        // Detection range
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Shooting range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootingRange);
        
        // Charge range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chargeRange);
        
        // Patrol path
        if (enablePatrol && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, patrolTargetPos);
            Gizmos.DrawWireSphere(patrolTargetPos, 0.3f);
        }
        
        // Line of sight
        if (player != null && Application.isPlaying)
        {
            Gizmos.color = HasLineOfSight() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }

    

    
}