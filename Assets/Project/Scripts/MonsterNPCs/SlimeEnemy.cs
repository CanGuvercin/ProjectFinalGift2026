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
    [SerializeField] private Vector2 patrolPointA = new Vector2(0, 2);
    [SerializeField] private Vector2 patrolPointB = new Vector2(0, -2);
    [SerializeField] private float patrolSpeed = 0.8f;
    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] private float patrolPointThreshold = 0.3f;

    [Header("Collision Detection")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Invulnerability")]
    [SerializeField] private float hitInvulnerableTime = 0.5f;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private CircleCollider2D circleCollider;

    // Patrol variables
    private Vector2 spawnPosition;
    private Vector2 currentPatrolTarget;
    private bool isWaitingAtPatrolPoint;
    private bool isGoingToA = true;

    private float lastShootTime;
    private bool isShooting;
    private bool isCharging;
    private bool isInvulnerable;
    private bool isKnockedBack;
    private bool isDead;

    private enum SlimeState { Idle, Patrolling, Shooting, Charging }
    private SlimeState currentState = SlimeState.Idle;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spawnPosition = transform.position;
        currentPatrolTarget = spawnPosition + patrolPointA;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (isDead) return;
        
        // Knockback sırasında sadece hareket durur, behavior devam eder
        if (!isKnockedBack)
        {
            UpdateBehavior();
        }
    }

    private void FixedUpdate()
    {
        if (isDead) return;

        // Knockback sırasında physics zaten KnockbackRoutine tarafından kontrol ediliyor
        if (!isKnockedBack)
        {
            HandleMovement();
        }
    }

    // ================= BEHAVIOR =================

    private void UpdateBehavior()
    {
        if (player == null)
        {
            if (enablePatrol)
                ChangeState(SlimeState.Patrolling);
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, player.position);
        bool seesPlayer = HasLineOfSight();

        if (enablePatrol && (!seesPlayer || distanceToPlayer > detectionRange))
        {
            ChangeState(SlimeState.Patrolling);
            return;
        }

        if (distanceToPlayer <= detectionRange && distanceToPlayer > shootingRange)
        {
            ChangeState(SlimeState.Idle);
            return;
        }

        if (distanceToPlayer <= shootingRange && distanceToPlayer > chargeRange)
        {
            ChangeState(SlimeState.Shooting);
            TryShoot();
            return;
        }

        if (distanceToPlayer <= chargeRange && !isCharging)
        {
            ChangeState(SlimeState.Charging);
            return;
        }
    }

    private void ChangeState(SlimeState newState)
    {
        if (currentState == newState) return;

        currentState = newState;

        switch (newState)
        {
            case SlimeState.Idle:
                rb.velocity = Vector2.zero;
                break;

            case SlimeState.Patrolling:
                UpdatePatrolTarget();
                break;

            case SlimeState.Shooting:
                rb.velocity = Vector2.zero;
                break;

            case SlimeState.Charging:
                rb.velocity = Vector2.zero;
                animator.SetTrigger("Charge");
                PlaySfx(chargeSfx);
                StopAllCoroutines();
                StartCoroutine(ChargeRoutine());
                break;
        }
    }

    // ================= PATROL =================

    private void UpdatePatrolTarget()
    {
        if (isGoingToA)
            currentPatrolTarget = spawnPosition + patrolPointA;
        else
            currentPatrolTarget = spawnPosition + patrolPointB;
    }

    private void HandleMovement()
    {
        if (isCharging)
            return;

        if (isShooting || currentState == SlimeState.Idle)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (currentState == SlimeState.Patrolling)
        {
            if (isWaitingAtPatrolPoint)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            float distanceToTarget = Vector2.Distance(transform.position, currentPatrolTarget);

            if (distanceToTarget <= patrolPointThreshold)
            {
                rb.velocity = Vector2.zero;
                StartCoroutine(PatrolWaitAndSwitch());
            }
            else
            {
                Vector2 direction = (currentPatrolTarget - (Vector2)transform.position).normalized;
                rb.velocity = direction * patrolSpeed;
            }
            return;
        }

        if (currentState == SlimeState.Shooting && !isShooting)
        {
            Vector2 direction = (player.position - transform.position).normalized;
            rb.velocity = direction * chaseSpeed;
        }
    }

    private IEnumerator PatrolWaitAndSwitch()
    {
        if (isWaitingAtPatrolPoint) yield break;
        
        isWaitingAtPatrolPoint = true;

        yield return new WaitForSeconds(patrolWaitTime);

        isGoingToA = !isGoingToA;
        UpdatePatrolTarget();

        isWaitingAtPatrolPoint = false;
    }

    // ================= LINE OF SIGHT =================

    private bool HasLineOfSight()
    {
        if (player == null) return false;

        Vector2 direction = (player.position - transform.position).normalized;
        float distance = Vector2.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, distance, obstacleLayer);

        return hit.collider == null;
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
            animator.SetTrigger("Shoot");
            yield return new WaitForSeconds(0.1f);

            ShootProjectile();
            //PlaySfx(shootSfx);
            // shoot sesi artık bullet prefab'da 27 dec 2025

            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstDelay);
        }

        yield return new WaitForSeconds(0.3f);
        isShooting = false;
    }

    private void ShootProjectile()
    {
        if (projectilePrefab == null || player == null) return;

        Vector2 direction = (player.position - transform.position).normalized;

        Transform spawnPoint = shootPoint != null ? shootPoint : transform;

        GameObject projectile = Instantiate(projectilePrefab,
            spawnPoint.position,
            Quaternion.identity);

        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.velocity = direction * projectileSpeed;
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
        ChangeState(SlimeState.Shooting);
    }

    // ================= DAMAGE SYSTEM =================

    public void TakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (isInvulnerable || isDead) return;

        currentHealth -= damage;
        
        // Önce animasyon ve ses
        animator.SetTrigger("Hit");
        PlaySfx(hitSfx);
        
        

        // Knockback yönü
        Vector2 knockbackDir = ((Vector2)transform.position - damageSourcePos).normalized;
        
        // Knockback başlat (coroutine)
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
        // İşaretleme
        isKnockedBack = true;

        // Mevcut coroutine'leri durdur (ama behavior devam etsin)
        if (isShooting)
        {
            StopCoroutine(ShootBurstRoutine());
            isShooting = false;
        }
        
        if (isCharging)
        {
            StopCoroutine(ChargeRoutine());
            isCharging = false;
        }

        // Knockback force uygula
        rb.velocity = direction * knockbackForce;
        
        

        // Knockback süresi
        yield return new WaitForSeconds(knockbackDuration);

        // Durdur
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

                // Slime de temastan geriye savrulsun!
                Vector2 knockbackDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
                
                
                
                // NOT: TakeDamage çağırmıyoruz çünkü slime player'a temas edince hasar almıyor
                // Sadece knockback yiyor
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
        Vector2 origin = Application.isPlaying ? spawnPosition : (Vector2)transform.position;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, shootingRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chargeRange);

        if (enablePatrol)
        {
            Vector2 pointA = origin + patrolPointA;
            Vector2 pointB = origin + patrolPointB;

            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(pointA, 0.3f);
            Gizmos.DrawWireSphere(pointB, 0.3f);
            Gizmos.DrawLine(pointA, pointB);

            if (Application.isPlaying)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, currentPatrolTarget);
            }
        }

        if (player != null && Application.isPlaying)
        {
            Gizmos.color = HasLineOfSight() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}