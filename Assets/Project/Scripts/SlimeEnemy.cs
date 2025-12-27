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

    [Header("Patrol Points")]
    [SerializeField] private Transform patrolPointA;
    [SerializeField] private Transform patrolPointB;
    [SerializeField] private float patrolSpeed = 0.8f;
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] private float patrolPointThreshold = 0.2f;



    [Header("Collision Detection")]
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Invulnerability")]
    [SerializeField] private float hitInvulnerableTime = 0.5f;


    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private CircleCollider2D circleCollider;

    private Transform currentPatrolTarget;
    private bool isWaitingAtPatrolPoint;

    private Coroutine patrolWaitRoutine;


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

        // Rigidbody ayarları
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        // Patrol başlangıç pozisyonu
        if (patrolPointA != null)
            currentPatrolTarget = patrolPointA;
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
    if (player == null)
    {
        if (enablePatrol)
            ChangeState(SlimeState.Patrolling);
        return;
    }

    float distanceToPlayer = Vector2.Distance(transform.position, player.position);
    bool seesPlayer = HasLineOfSight();

    // 1️⃣ PLAYER GÖRÜNMÜYOR → PATROL
    if (enablePatrol && (!seesPlayer || distanceToPlayer > detectionRange))
    {
        ChangeState(SlimeState.Patrolling);
        return;
    }

    // 2️⃣ PLAYER VAR AMA UZAK → IDLE
    if (distanceToPlayer <= detectionRange && distanceToPlayer > shootingRange)
    {
        ChangeState(SlimeState.Idle);
        return;
    }

    // 3️⃣ SHOOT
    if (distanceToPlayer <= shootingRange && distanceToPlayer > chargeRange)
    {
        ChangeState(SlimeState.Shooting);
        TryShoot();
        return;
    }

    // 4️⃣ CHARGE
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

        // Animasyon trigger'ları
        switch (newState)
        {
            case SlimeState.Idle:
                rb.velocity = Vector2.zero;
                break;

            case SlimeState.Patrolling:
                if (currentPatrolTarget == null)
                    currentPatrolTarget = patrolPointA;
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


 private void HandlePatrol()
{
    if (currentPatrolTarget == null || isWaitingAtPatrolPoint)
        return;

    float distance =
        Vector2.Distance(transform.position, currentPatrolTarget.position);

    if (distance <= patrolPointThreshold)
    {
        // 🛑 DURSUN
        rb.velocity = Vector2.zero;

        // ⏳ BEKLE + TARGET DEĞİŞTİR
        patrolWaitRoutine = StartCoroutine(PatrolWaitAndSwitch());
    }
}

private IEnumerator PatrolWaitAndSwitch()
{
    isWaitingAtPatrolPoint = true;

    yield return new WaitForSeconds(patrolWaitTime);

    // A <-> B swap
    currentPatrolTarget =
        currentPatrolTarget == patrolPointA
            ? patrolPointB
            : patrolPointA;

    isWaitingAtPatrolPoint = false;
}





    private IEnumerator PatrolWaitRoutine()
    {
        isWaitingAtPatrolPoint = true;
        rb.velocity = Vector2.zero;

        yield return new WaitForSeconds(patrolWaitTime);

        // Target değiştir
        currentPatrolTarget =
            currentPatrolTarget == patrolPointA ? patrolPointB : patrolPointA;

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
        if (isCharging)
            return;

        if (isShooting || currentState == SlimeState.Idle)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (currentState == SlimeState.Patrolling)
        {
            if (currentPatrolTarget == null)
            {
                rb.velocity = Vector2.zero;
                return;
            }

            Vector2 direction =
                ((Vector2)currentPatrolTarget.position - (Vector2)transform.position).normalized;

            rb.velocity = direction * patrolSpeed;
            return;
        }

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
            return;
        }

        if (player == null)
        {
            return;
        }

        Vector2 direction = (player.position - transform.position).normalized;

        Transform spawnPoint = shootPoint != null ? shootPoint : transform;


        GameObject projectile = Instantiate(projectilePrefab,
        transform.position + (Vector3)direction * 0.5f,
        Quaternion.identity);


        Rigidbody2D projRb = projectile.GetComponent<Rigidbody2D>();
        if (projRb != null)
        {
            projRb.velocity = direction * projectileSpeed;
        }
        else
        {
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
            if (currentPatrolTarget != null)
            {
                Gizmos.DrawLine(transform.position, currentPatrolTarget.position);
                Gizmos.DrawWireSphere(currentPatrolTarget.position, 0.3f);
            }

        }

        // Line of sight
        if (player != null && Application.isPlaying)
        {
            Gizmos.color = HasLineOfSight() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, player.position);
        }
    }
}