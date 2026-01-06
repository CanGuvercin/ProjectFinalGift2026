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

    // patrol
    private Vector2 spawnPos;
    private Vector2 currentPatrolTarget;
    private bool isGoingToA = true;
    private bool isWaitingAtPatrolPoint;

    // state
    private enum State { Patrol, Chase, Shoot, Charge, Retreat, Investigate }
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
        if (!player)
        {
            state = enablePatrol ? State.Patrol : State.Investigate;
            return;
        }

        float dist = Vector2.Distance(transform.position, player.position);
        bool sees = HasLineOfSight();

        bool hitRecently = (Time.time - lastHitTime) <= hitMemoryTime;

        // 1) Retreat: dayak + çok yakın
        if (!isDead && hitRecently && dist <= retreatTriggerDistance && state != State.Charge)
        {
            StartRetreat();
            return;
        }

        // 2) Charge: yakın + cooldown bitti + (sees veya alerted taze)
        bool canCharge = Time.time >= nextChargeTime;
        bool hasRecentInfo = sees || (isAlerted && (Time.time - lastSeenTime) < 1.0f);

        if (dist <= chargeRange && canCharge && hasRecentInfo && state != State.Retreat)
        {
            StartCharge();
            return;
        }

        // 3) Shoot: LoS varsa ve (normal menzil) ya da (alerted ise uzun menzil)
        bool canShoot = Time.time >= nextShootTime;
        bool inNormalShoot = dist <= shootRange;
        bool inAlertShoot = isAlerted && dist <= alertedShootRange;

        if (sees && canShoot && (inNormalShoot || inAlertShoot) && state != State.Charge)
        {
            StartShoot();
            return;
        }

        // 4) Chase / Investigate:
        if (sees)
        {
            state = State.Chase;
            return;
        }

        // sees yok ama alerted var: lastKnownPlayerPos'a investigate
        if (isAlerted)
        {
            state = State.Investigate;
            return;
        }

        // 5) Patrol
        state = enablePatrol ? State.Patrol : State.Investigate;
    }

    // ================== MOVEMENT ==================

    private void ApplyMovement()
    {
        switch (state)
        {
            case State.Patrol:
                PatrolMove();
                break;

            case State.Chase:
                MoveToward(player.position, chaseSpeed);
                break;

            case State.Investigate:
                // lastKnownPlayerPos'a doğru
                if (Vector2.Distance(transform.position, lastKnownPlayerPos) <= investigateStopDistance)
                    rb.velocity = Vector2.zero;
                else
                    MoveToward(lastKnownPlayerPos, chaseSpeed);
                break;

            case State.Shoot:
                // shoot sırasında dur (istersen strafe ekleriz)
                rb.velocity = Vector2.zero;
                break;

            case State.Charge:
                // chargeCo rb.velocity set ediyor
                break;

            case State.Retreat:
                // retreatCo rb.velocity set ediyor
                break;
        }
    }

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.velocity = dir * speed;
    }

    private void PatrolMove()
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

        float d = Vector2.Distance(transform.position, currentPatrolTarget);
        if (d <= patrolPointThreshold)
        {
            rb.velocity = Vector2.zero;
            if (patrolWaitCo == null)
                patrolWaitCo = StartCoroutine(PatrolWaitAndSwitch());
            return;
        }

        MoveToward(currentPatrolTarget, patrolSpeed);
    }

    private IEnumerator PatrolWaitAndSwitch()
    {
        isWaitingAtPatrolPoint = true;
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
    }
}
