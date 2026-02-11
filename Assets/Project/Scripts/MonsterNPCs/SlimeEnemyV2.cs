using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class SlimeEnemyV2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform shootPoint;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private AudioSource audioSource;

    [Header("SFX")]
    [SerializeField] private AudioClip hitSfx;
    [SerializeField] private AudioClip chargeSfx;
    [SerializeField] private AudioClip dieSfx;
    // shootSfx yok: sende mermi prefab'ında (27 Dec 2025) diyordun.

    [Header("Combat Stats")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int contactDamage = 10;
    [SerializeField] private float knockbackForce = 5f;
    [SerializeField] private float knockbackDuration = 0.3f;
    [SerializeField] private float hitInvulnerableTime = 0.5f;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private float shootRange = 6f;                 // normal shoot
    [SerializeField] private float alertedShootRange = 12f;         // "ebesinin nikahı" (LoS şart)
    [SerializeField] private float chargeRange = 3f;

    [Header("Movement")]
    [SerializeField] private float patrolSpeed = 0.8f;
    [SerializeField] private float chaseSpeed = 1.5f;
    [SerializeField] private float retreatSpeed = 1.7f;
    [SerializeField] private float chargeSpeed = 4f;

    [Header("Shooting")]
    [SerializeField] private int burstCount = 2;
    [SerializeField] private float burstDelay = 0.3f;
    [SerializeField] private float shootCooldown = 2f;
    [SerializeField] private float projectileSpeed = 5f;

    [Header("Charge")]
    [SerializeField] private float chargeDuration = 0.8f;
    [SerializeField] private float chargeCooldown = 2.8f;

    [Header("Aggro Memory")]
    [SerializeField] private float forgetAfter = 6f;     // player kaybolursa kaç sn sonra sakinleşsin
    [SerializeField] private float investigateStopDistance = 0.5f;

    [Header("Retreat (Hit Reaction)")]
    [SerializeField] private float hitMemoryTime = 2.0f;        // "yakın zamanda dayak yediyse"
    [SerializeField] private float retreatDuration = 1.1f;
    [SerializeField] private float retreatTriggerDistance = 3.2f; // player bu kadar yakınken dayak yedi -> retreat
    [SerializeField] private bool shootWhileRetreating = true;

    [Header("Patrol")]
    [SerializeField] private bool enablePatrol = true;
    [SerializeField] private Vector2 patrolPointA = new Vector2(0, 2);
    [SerializeField] private Vector2 patrolPointB = new Vector2(0, -2);
    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] private float patrolPointThreshold = 0.3f;

    [Header("Enemy Separation")]
    [SerializeField] private float separationDistance = 1.5f;      // bu kadar yakında başka enemy varsa uzaklaş
    [SerializeField] private float separationForce = 2.5f;         // uzaklaşma hızı
    [SerializeField] private float separationDuration = 0.8f;      // ne kadar süre uzaklaşsın
    [SerializeField] private float separationCheckInterval = 0.3f; // kaç saniyede bir kontrol et

    [Header("Collision / LOS")]
    [SerializeField] private LayerMask obstacleLayer;

    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private CircleCollider2D circleCollider;

    private int currentHealth;
    private bool isDead;
    private bool isInvulnerable;
    private bool isKnockedBack;

    // memory
    private bool isAlerted;
    private float lastSeenTime;
    private Vector2 lastKnownPlayerPos;

    // timers
    private float nextShootTime;
    private float nextChargeTime;
    private float lastHitTime;
    private float nextSeparationCheckTime;

    // patrol
    private Vector2 spawnPos;
    private Vector2 currentPatrolTarget;
    private bool isGoingToA = true;
    private bool isWaitingAtPatrolPoint;

    // separation
    private bool isSeparating;
    private Vector2 separationDirection;
    private float separationEndTime;

    // state
    private enum State { Patrol, Chase, Shoot, Charge, Retreat, Investigate, Separate }
    private State state = State.Patrol;

    // coroutine handles
    private Coroutine shootCo;
    private Coroutine chargeCo;
    private Coroutine patrolWaitCo;
    private Coroutine retreatCo;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();

        currentHealth = maxHealth;

        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        spawnPos = transform.position;
        currentPatrolTarget = spawnPos + patrolPointA;
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p) player = p.transform;
    }

    private void Update()
    {
        if (isDead) return;

        // Separation kontrolü (periyodik)
        if (Time.time >= nextSeparationCheckTime)
        {
            CheckForEnemySeparation();
            nextSeparationCheckTime = Time.time + separationCheckInterval;
        }

        // Separation aktifse süre kontrolü
        if (isSeparating && Time.time >= separationEndTime)
        {
            isSeparating = false;
        }

        // Knockback sırasında physics'i KnockbackRoutine yönetiyor; ama karar vermeyi tamamen kesmiyoruz.
        UpdatePerceptionAndMemory();
        UpdateDecision();
    }

    private void FixedUpdate()
    {
        if (isDead) return;
        if (isKnockedBack) return;

        ApplyMovement();
    }

    // ================== SEPARATION SYSTEM ==================

    private void CheckForEnemySeparation()
    {
        if (isDead || isKnockedBack) return;

        // Çevredeki tüm enemy'leri bul
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationDistance);
        
        Vector2 totalSeparationDir = Vector2.zero;
        int enemyCount = 0;

        foreach (Collider2D col in nearbyEnemies)
        {
            // Kendini ve player'ı atla
            if (col.gameObject == gameObject) continue;
            if (!col.CompareTag("Enemy")) continue;

            // Bu enemy'den uzaklaş
            Vector2 awayDir = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
            totalSeparationDir += awayDir;
            enemyCount++;
        }

        // Eğer yakında enemy varsa separation başlat
        if (enemyCount > 0)
        {
            separationDirection = totalSeparationDir.normalized;
            isSeparating = true;
            separationEndTime = Time.time + separationDuration;

            // Separation yüzünden mevcut saldırıları kesme, ama state'i güncelle
            if (state != State.Charge && state != State.Retreat)
            {
                state = State.Separate;
            }
        }
    }

    // ================== PERCEPTION + MEMORY ==================

    private void UpdatePerceptionAndMemory()
    {
        if (!player) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool inDetect = dist <= detectionRange;
        bool sees = inDetect && HasLineOfSight();

        if (sees)
        {
            isAlerted = true;
            lastSeenTime = Time.time;
            lastKnownPlayerPos = player.position;
        }
        else
        {
            // unutma
            if (isAlerted && (Time.time - lastSeenTime) > forgetAfter)
            {
                isAlerted = false;
            }
        }
    }

    private bool HasLineOfSight()
    {
        if (!player) return false;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        float dist = Vector2.Distance(transform.position, player.position);

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, dist, obstacleLayer);
        return hit.collider == null;
    }

    // ================== DECISION ==================

    private void UpdateDecision()
    {
        if (!player) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // aktif coroutine varsa bekle
        if (shootCo != null || chargeCo != null || patrolWaitCo != null || retreatCo != null) return;

        // Separation aktifse çok müdahale etme
        if (isSeparating)
        {
            if (state != State.Charge && state != State.Retreat)
                state = State.Separate;
            return;
        }

        // 1) RETREAT (yeni dayak yedi ve player çok yakın)
        if (WantsToRetreat(dist))
        {
            StartRetreat();
            return;
        }

        // 2) ALERTED
        if (isAlerted)
        {
            bool sees = HasLineOfSight();

            // a) Charge mesafesi
            if (sees && dist <= chargeRange && Time.time >= nextChargeTime)
            {
                StartCharge();
                return;
            }

            // b) Shoot mesafesi
            bool inShoot = sees && dist <= shootRange;
            // veya alertedShootRange içinde ve LoS var
            bool inAlertedShoot = sees && dist <= alertedShootRange;

            if ((inShoot || inAlertedShoot) && Time.time >= nextShootTime)
            {
                StartShoot();
                return;
            }

            // c) Chase / Investigate
            if (sees)
            {
                state = State.Chase;
            }
            else
            {
                // lastKnownPlayerPos'a git
                float distToLast = Vector2.Distance(transform.position, lastKnownPlayerPos);
                if (distToLast > investigateStopDistance)
                    state = State.Investigate;
                else
                    state = State.Patrol; // artık unut
            }
        }
        else
        {
            // 3) NOT ALERTED -> Patrol
            state = State.Patrol;
        }
    }

    private bool WantsToRetreat(float distToPlayer)
    {
        // yakın zamanda dayak yedi mi?
        if (Time.time - lastHitTime > hitMemoryTime) return false;
        // player yeterince yakın mı?
        return distToPlayer <= retreatTriggerDistance;
    }

    // ================== MOVEMENT ==================

    private void ApplyMovement()
    {
        // Separation devredeyse öncelik ona
        if (isSeparating)
        {
            rb.velocity = separationDirection * separationForce;
            return;
        }

        switch (state)
        {
            case State.Patrol:
                Patrol();
                break;

            case State.Chase:
                ChasePlayer();
                break;

            case State.Investigate:
                Investigate();
                break;

            case State.Shoot:
            case State.Charge:
            case State.Retreat:
            case State.Separate:
                // zaten coroutine kontrolü
                break;
        }
    }

    private void Patrol()
    {
        if (!enablePatrol)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (isWaitingAtPatrolPoint)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        Vector2 dir = (currentPatrolTarget - (Vector2)transform.position).normalized;
        rb.velocity = dir * patrolSpeed;

        float dist = Vector2.Distance(transform.position, currentPatrolTarget);
        if (dist < patrolPointThreshold)
        {
            ReachedPatrolPoint();
        }
    }

    private void ChasePlayer()
    {
        if (!player) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.velocity = dir * chaseSpeed;
    }

    private void Investigate()
    {
        Vector2 dir = (lastKnownPlayerPos - (Vector2)transform.position).normalized;
        rb.velocity = dir * chaseSpeed;
    }

    private void ReachedPatrolPoint()
    {
        isWaitingAtPatrolPoint = true;
        rb.velocity = Vector2.zero;

        patrolWaitCo = StartCoroutine(PatrolWaitRoutine());
    }

    private IEnumerator PatrolWaitRoutine()
    {
        yield return new WaitForSeconds(patrolWaitTime);

        isGoingToA = !isGoingToA;
        currentPatrolTarget = spawnPos + (isGoingToA ? patrolPointA : patrolPointB);

        isWaitingAtPatrolPoint = false;
        patrolWaitCo = null;
    }

    // ================== ACTION STARTERS ==================

    private void StartShoot()
    {
        if (shootCo != null) return;

        state = State.Shoot;
        rb.velocity = Vector2.zero; // Dur! Ateş ederken hareket etme
        shootCo = StartCoroutine(ShootBurstRoutine());
    }

    private IEnumerator ShootBurstRoutine()
    {
        nextShootTime = Time.time + shootCooldown;

        for (int i = 0; i < burstCount; i++)
        {
            animator.SetTrigger("Shoot");
            yield return new WaitForSeconds(0.1f);

            ShootProjectile();

            if (i < burstCount - 1)
                yield return new WaitForSeconds(burstDelay);
        }

        yield return new WaitForSeconds(0.15f);
        shootCo = null;
    }

    private void ShootProjectile()
    {
        if (isDead) return; // ← EKLENDI!
        if (!projectilePrefab || !player) return;

        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        Transform sp = shootPoint ? shootPoint : transform;

        GameObject projectile = Instantiate(projectilePrefab, sp.position, Quaternion.identity);

        Rigidbody2D prb = projectile.GetComponent<Rigidbody2D>();
        if (prb) prb.velocity = dir * projectileSpeed;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        projectile.transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void StartCharge()
    {
        if (chargeCo != null) return;

        state = State.Charge;
        animator.SetTrigger("Charge");
        PlaySfx(chargeSfx);

        nextChargeTime = Time.time + chargeCooldown;

        // shoot varsa kes
        if (shootCo != null) { StopCoroutine(shootCo); shootCo = null; }

        chargeCo = StartCoroutine(ChargeRoutine());
    }

    private IEnumerator ChargeRoutine()
    {
        Vector2 dir = player ? ((Vector2)player.position - (Vector2)transform.position).normalized : Vector2.zero;

        float t = 0f;
        while (t < chargeDuration)
        {
            rb.velocity = dir * chargeSpeed;
            t += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;

        // kısa toparlanma
        yield return new WaitForSeconds(0.2f);

        chargeCo = null;
        // decision tekrar çalışsın diye state'i zorlamıyoruz
    }

    private void StartRetreat()
    {
        if (retreatCo != null) return;

        // charge varsa kes
        if (chargeCo != null) { StopCoroutine(chargeCo); chargeCo = null; }
        // shoot varsa kes (istersen kesme; ben burada kesiyorum ki retreat hissi net olsun)
        if (shootCo != null) { StopCoroutine(shootCo); shootCo = null; }

        state = State.Retreat;
        retreatCo = StartCoroutine(RetreatRoutine());
    }

    private IEnumerator RetreatRoutine()
    {
        float endTime = Time.time + retreatDuration;

        while (Time.time < endTime && player != null)
        {
            Vector2 away = ((Vector2)transform.position - (Vector2)player.position).normalized;
            rb.velocity = away * retreatSpeed;

            if (shootWhileRetreating && HasLineOfSight() && Time.time >= nextShootTime)
            {
                StartShoot();
            }

            yield return null;
        }

        rb.velocity = Vector2.zero;
        retreatCo = null;
    }

    // ================== DAMAGE ==================

    public void TakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (isDead || isInvulnerable) return;

        currentHealth -= damage;
        lastHitTime = Time.time;

        animator.SetTrigger("Hit");
        PlaySfx(hitSfx);

        // aggro garantile
        isAlerted = true;
        lastSeenTime = Time.time;
        if (player) lastKnownPlayerPos = player.position;

        Vector2 kbDir = ((Vector2)transform.position - damageSourcePos).normalized;
        StartCoroutine(KnockbackRoutine(kbDir));

        if (currentHealth <= 0) Die();
        else StartCoroutine(InvulnerabilityRoutine());
    }

    private IEnumerator KnockbackRoutine(Vector2 direction)
    {
        isKnockedBack = true;

        // knockback sırasında aktif saldırıları kes
        if (shootCo != null) { StopCoroutine(shootCo); shootCo = null; }
        if (chargeCo != null) { StopCoroutine(chargeCo); chargeCo = null; }

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
        
        // TÜM COROUTINE'LERİ DURDUR - EKLENDI!
        if (shootCo != null) { StopCoroutine(shootCo); shootCo = null; }
        if (chargeCo != null) { StopCoroutine(chargeCo); chargeCo = null; }
        if (retreatCo != null) { StopCoroutine(retreatCo); retreatCo = null; }
        if (patrolWaitCo != null) { StopCoroutine(patrolWaitCo); patrolWaitCo = null; }
        
        animator.SetTrigger("Die");
        PlaySfx(dieSfx);

        rb.velocity = Vector2.zero;
        circleCollider.enabled = false;

        Destroy(gameObject, 1f);
    }

    // ================== COLLISION ==================

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isDead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerController pc = collision.gameObject.GetComponent<PlayerController>();
            if (pc != null)
            {
                pc.TakeDamage(contactDamage, transform.position);

                // slime temas edince geri savrulsun
                Vector2 kbDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
                StartCoroutine(KnockbackRoutine(kbDir));
            }
        }
        // Enemy ile çarpışmada separation'ı tetikle
        else if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 awayDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
            separationDirection = awayDir;
            isSeparating = true;
            separationEndTime = Time.time + separationDuration;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerAttack"))
        {
            TakeDamage(10, other.transform.position);
        }
    }

    // ================== AUDIO ==================

    private void PlaySfx(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.pitch = Random.Range(0.95f, 1.05f);
            audioSource.PlayOneShot(clip);
        }
    }

    // ================== DEBUG ==================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;   Gizmos.DrawWireSphere(transform.position, detectionRange);
        Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, shootRange);
        Gizmos.color = new Color(1f, 0.5f, 0f); Gizmos.DrawWireSphere(transform.position, alertedShootRange);
        Gizmos.color = Color.red;    Gizmos.DrawWireSphere(transform.position, chargeRange);
        Gizmos.color = Color.magenta; Gizmos.DrawWireSphere(transform.position, separationDistance);
    }
}