using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class BomberSlime : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    
    [Header("SFX")]
    [SerializeField] private AudioClip chargeSfx;
    [Tooltip("Hücum başladığında çalan ses")]
    [SerializeField] private AudioClip explosionSfx;
    [Tooltip("Patlama sesi")]
    
    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [Tooltip("Ali'yi bu mesafede algılar")]
    [SerializeField] private LayerMask obstacleLayer;
    [Tooltip("Line of Sight için obstacle kontrolü")]
    
    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 3.5f;
    [Tooltip("Hücum süresi (saniye)")]
    [SerializeField] private float chargeSpeed = 4f;
    [Tooltip("Hücum hızı")]
    
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 3f;
    [Tooltip("Patlama yarıçapı")]
    [SerializeField] private int explosionDamage = 100;
    [Tooltip("Patlama hasarı")]
    [SerializeField] private float destroyDelay = 0.5f;
    [Tooltip("Boom animasyonu bittikten sonra destroy gecikmesi")]
    
    [Header("Patrol (Idle)")]
    [SerializeField] private float idleSpeed = 0.6f;
    [Tooltip("Normal dolaşma hızı")]
    [SerializeField] private Vector2 patrolPointA = new Vector2(-2, 0);
    [SerializeField] private Vector2 patrolPointB = new Vector2(2, 0);
    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] private float patrolPointThreshold = 0.3f;
    
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private CircleCollider2D circleCollider;
    
    private Vector2 spawnPos;
    private Vector2 currentPatrolTarget;
    private bool isGoingToA = true;
    private bool isWaitingAtPatrolPoint;
    private Coroutine patrolWaitCo;
    
    private enum State { Idle, Charging, PreBoom, Boom }
    private State state = State.Idle;
    
    private bool isExploding = false;
    private float chargeTimer = 0f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>(); // Child'dan al
        circleCollider = GetComponent<CircleCollider2D>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        
        spawnPos = transform.position;
        currentPatrolTarget = spawnPos + patrolPointA;
        
        Debug.Log($"[BomberSlime] 💣 Spawned at {spawnPos}");
    }
    
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj) player = playerObj.transform;
    }
    
    private void Update()
    {
        if (isExploding || player == null) return;
        
        // State machine
        switch (state)
        {
            case State.Idle:
                CheckForPlayer();
                break;
                
            case State.Charging:
                UpdateCharge();
                break;
                
            case State.PreBoom:
            case State.Boom:
                // Animasyon oynarken hareket etme
                rb.velocity = Vector2.zero;
                break;
        }
    }
    
    private void FixedUpdate()
    {
        if (isExploding) return;
        
        ApplyMovement();
        UpdateAnimator();
    }
    
    // ================== DETECTION ==================
    
    private void CheckForPlayer()
    {
        if (player == null) return;
        
        float distToPlayer = Vector2.Distance(transform.position, player.position);
        
        if (distToPlayer <= detectionRange && HasLineOfSight(player.position))
        {
            StartCharge();
        }
    }
    
    private bool HasLineOfSight(Vector2 targetPos)
    {
        Vector2 origin = transform.position;
        Vector2 dir = targetPos - origin;
        float dist = dir.magnitude;
        
        RaycastHit2D hit = Physics2D.Raycast(origin, dir.normalized, dist, obstacleLayer);
        return hit.collider == null; // Obstacle yoksa true
    }
    
    // ================== CHARGE ==================
    
    private void StartCharge()
    {
        state = State.Charging;
        chargeTimer = chargeDuration;
        
        Debug.Log($"[BomberSlime] 🔥 Charging at player! Duration: {chargeDuration}s");
        
        // Charge SFX
        if (audioSource && chargeSfx)
        {
            audioSource.PlayOneShot(chargeSfx);
        }
        
        // Animator
        animator.SetBool("isCharging", true);
    }
    
    private void UpdateCharge()
    {
        chargeTimer -= Time.deltaTime;
        
        // Süre doldu mu?
        if (chargeTimer <= 0f)
        {
            Debug.Log("[BomberSlime] ⏰ Charge time expired! Self-destructing...");
            TriggerExplosion();
        }
    }
    
    // ================== MOVEMENT ==================
    
    private void ApplyMovement()
    {
        switch (state)
        {
            case State.Idle:
                PatrolMove();
                break;
                
            case State.Charging:
                ChargeMove();
                break;
                
            case State.PreBoom:
            case State.Boom:
                rb.velocity = Vector2.zero;
                break;
        }
    }
    
    private void PatrolMove()
    {
        if (isWaitingAtPatrolPoint)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        Vector2 targetWorldPos = spawnPos + currentPatrolTarget;
        float dist = Vector2.Distance(transform.position, targetWorldPos);
        
        if (dist <= patrolPointThreshold)
        {
            rb.velocity = Vector2.zero;
            if (patrolWaitCo == null)
            {
                patrolWaitCo = StartCoroutine(PatrolWaitRoutine());
            }
        }
        else
        {
            MoveToward(targetWorldPos, idleSpeed);
        }
    }
    
    private IEnumerator PatrolWaitRoutine()
    {
        isWaitingAtPatrolPoint = true;
        yield return new WaitForSeconds(patrolWaitTime);
        
        // Swap target
        if (isGoingToA)
            currentPatrolTarget = patrolPointB;
        else
            currentPatrolTarget = patrolPointA;
        
        isGoingToA = !isGoingToA;
        isWaitingAtPatrolPoint = false;
        patrolWaitCo = null;
    }
    
    private void ChargeMove()
    {
        if (player == null)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        
        MoveToward(player.position, chargeSpeed);
    }
    
    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = ((Vector2)target - (Vector2)transform.position).normalized;
        rb.velocity = dir * speed;
    }
    
    // ================== COLLISION ==================
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isExploding) return;
        
        // Charging sırasında Player'a çarptı mı?
        if (state == State.Charging && collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("[BomberSlime] 💥 Hit player! Triggering explosion...");
            TriggerExplosion();
        }
    }
    
    // ================== EXPLOSION ==================
    
    private void TriggerExplosion()
    {
        if (isExploding) return;
        
        isExploding = true;
        state = State.PreBoom;
        rb.velocity = Vector2.zero;
        
        Debug.Log("[BomberSlime] ⚠️ PreBoom state");
        
        // PreBoom animation (2 frame - çok kısa)
        animator.SetTrigger("PreBoom");
        
        // PreBoom bitince Boom'a geç (kısa delay)
        StartCoroutine(PreBoomToBooomRoutine());
    }
    
    private IEnumerator PreBoomToBooomRoutine()
    {
        // PreBoom animasyonu çok kısa (2 frame)
        yield return new WaitForSeconds(0.1f);
        
        state = State.Boom;
        
        Debug.Log("[BomberSlime] 💥 BOOM!");
        
        // Boom animation
        animator.SetTrigger("Boom");
        
        // Explosion SFX
        if (audioSource && explosionSfx)
        {
            audioSource.PlayOneShot(explosionSfx);
        }
        
        // Damage everything in radius
        DealExplosionDamage();
        
        // Destroy after animation
        Destroy(gameObject, destroyDelay);
    }
    
    private void DealExplosionDamage()
    {
        Vector2 explosionCenter = transform.position;
        
        // Find all colliders in radius
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionCenter, explosionRadius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue; // Skip self
            
            // Damage Player
            if (hit.CompareTag("Player"))
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TakeDamage(explosionDamage, explosionCenter);
                    Debug.Log($"[BomberSlime] 💥 Player took {explosionDamage} explosion damage!");
                }
            }
            
            // Damage other SlimeEnemyV2
            SlimeEnemyV2 slime = hit.GetComponent<SlimeEnemyV2>();
            if (slime != null)
            {
                slime.TakeDamage(explosionDamage, explosionCenter);
                Debug.Log($"[BomberSlime] 💥 {slime.name} took {explosionDamage} damage!");
            }
            
            // Damage other BomberSlimes (chain reaction!)
            BomberSlime bomber = hit.GetComponent<BomberSlime>();
            if (bomber != null && bomber != this && !bomber.isExploding)
            {
                bomber.TriggerExplosion();
                Debug.Log($"[BomberSlime] 💥💥 Chain reaction! {bomber.name} triggered!");
            }
        }
    }
    
    // ================== ANIMATOR ==================
    
    private void UpdateAnimator()
    {
        bool isMoving = rb.velocity.magnitude > 0.1f;
        animator.SetBool("isMoving", isMoving);
        
        // Flip sprite based on velocity (child transform'u flip et)
        if (Mathf.Abs(rb.velocity.x) > 0.01f)
        {
            Transform spriteTransform = animator.transform; // Animator child'da
            spriteTransform.localScale = new Vector3(
                rb.velocity.x > 0 ? 1 : -1,
                1, 1
            );
        }
    }
    
    // ================== GIZMOS ==================
    
    private void OnDrawGizmosSelected()
    {
        // Detection range (blue)
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        // Explosion radius (red)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        // Patrol points (green)
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPos + patrolPointA, 0.3f);
            Gizmos.DrawWireSphere(spawnPos + patrolPointB, 0.3f);
        }
    }
}//