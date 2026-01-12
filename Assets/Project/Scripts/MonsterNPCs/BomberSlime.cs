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
    [SerializeField] private AudioClip explosionSfx;
    
    [Header("Detection")]
    [SerializeField] private float detectionRange = 8f;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Charge Settings")]
    [SerializeField] private float chargeDuration = 3.5f;
    [SerializeField] private float chargeSpeed = 4f;
    
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 3f;
    [SerializeField] private int explosionDamage = 100;
    [SerializeField] private float destroyDelay = 1f;
    
    [Header("Patrol (Idle)")]
    [SerializeField] private bool enablePatrol = false;
    [SerializeField] private float idleSpeed = 0.6f;
    [SerializeField] private Vector2 patrolPointA = new Vector2(-2, 0);
    [SerializeField] private Vector2 patrolPointB = new Vector2(2, 0);
    [SerializeField] private float patrolWaitTime = 1.5f;
    [SerializeField] private float patrolPointThreshold = 0.3f;
    
    [Header("Enemy Separation")]
    [SerializeField] private float separationDistance = 1.5f;
    [SerializeField] private float separationForce = 2.5f;
    [SerializeField] private float separationDuration = 0.8f;
    [SerializeField] private float separationCheckInterval = 0.3f;
    
    private Transform player;
    private Rigidbody2D rb;
    private Animator animator;
    private CircleCollider2D circleCollider;
    
    private Vector2 spawnPos;
    private Vector2 patrolTargetA;
    private Vector2 patrolTargetB;
    private Vector2 currentPatrolTarget;
    private bool isGoingToA = true;
    private bool isWaitingAtPatrolPoint;
    private Coroutine patrolWaitCo;
    
    private float nextSeparationCheckTime;
    private bool isSeparating;
    private Vector2 separationDirection;
    private float separationEndTime;
    
    private enum State { Idle, Charging, Exploding }
    private State state = State.Idle;
    
    private bool isExploding = false;
    private float chargeTimer = 0f;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponentInChildren<Animator>();
        circleCollider = GetComponent<CircleCollider2D>();
        if (!audioSource) audioSource = GetComponent<AudioSource>();
        
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        
        spawnPos = transform.position;
        
        if (enablePatrol)
        {
            patrolTargetA = spawnPos + patrolPointA;
            patrolTargetB = spawnPos + patrolPointB;
            currentPatrolTarget = patrolTargetA;
        }
    }
    
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj)
        {
            player = playerObj.transform;
        }
    }
    
    private void Update()
    {
        if (isExploding || player == null) return;
        
        if (enablePatrol && Time.time >= nextSeparationCheckTime)
        {
            CheckForEnemySeparation();
            nextSeparationCheckTime = Time.time + separationCheckInterval;
        }
        
        if (isSeparating && Time.time >= separationEndTime)
        {
            isSeparating = false;
        }
        
        switch (state)
        {
            case State.Idle:
                CheckForPlayer();
                break;
                
            case State.Charging:
                UpdateCharge();
                break;
                
            case State.Exploding:
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
    
    private void CheckForEnemySeparation()
    {
        if (isExploding) return;
        
        Collider2D[] nearbyEnemies = Physics2D.OverlapCircleAll(transform.position, separationDistance);
        
        Vector2 totalSeparationDir = Vector2.zero;
        int enemyCount = 0;
        
        foreach (Collider2D col in nearbyEnemies)
        {
            if (col.gameObject == gameObject) continue;
            if (!col.CompareTag("Enemy")) continue;
            
            Vector2 awayDir = ((Vector2)transform.position - (Vector2)col.transform.position).normalized;
            totalSeparationDir += awayDir;
            enemyCount++;
        }
        
        if (enemyCount > 0)
        {
            separationDirection = totalSeparationDir.normalized;
            isSeparating = true;
            separationEndTime = Time.time + separationDuration;
        }
    }
    
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
        return hit.collider == null;
    }
    
    private void StartCharge()
    {
        state = State.Charging;
        chargeTimer = chargeDuration;
        
        if (audioSource && chargeSfx)
        {
            audioSource.PlayOneShot(chargeSfx);
        }
        
        if (animator)
        {
            animator.SetBool("Charge", true);
        }
    }
    
    private void UpdateCharge()
    {
        chargeTimer -= Time.deltaTime;
        
        if (chargeTimer <= 0f)
        {
            TriggerExplosion();
        }
    }
    
    private void ApplyMovement()
    {
        if (enablePatrol && isSeparating && state == State.Idle)
        {
            rb.velocity = separationDirection * separationForce;
            return;
        }
        
        switch (state)
        {
            case State.Idle:
                if (enablePatrol)
                {
                    PatrolMove();
                }
                else
                {
                    rb.velocity = Vector2.zero;
                }
                break;
                
            case State.Charging:
                ChargeMove();
                break;
                
            case State.Exploding:
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
        
        float dist = Vector2.Distance(transform.position, currentPatrolTarget);
        
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
            Vector2 targetDir = (currentPatrolTarget - (Vector2)transform.position).normalized;
            rb.velocity = targetDir * idleSpeed;
        }
    }
    
    private IEnumerator PatrolWaitRoutine()
    {
        isWaitingAtPatrolPoint = true;
        yield return new WaitForSeconds(patrolWaitTime);
        
        currentPatrolTarget = isGoingToA ? patrolTargetB : patrolTargetA;
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
        
        Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
        rb.velocity = dir * chargeSpeed;
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (isExploding) return;
        
        if (state == State.Charging && collision.gameObject.CompareTag("Player"))
        {
            TriggerExplosion();
        }
        else if (enablePatrol && collision.gameObject.CompareTag("Enemy") && state == State.Idle)
        {
            Vector2 awayDir = ((Vector2)transform.position - (Vector2)collision.transform.position).normalized;
            separationDirection = awayDir;
            isSeparating = true;
            separationEndTime = Time.time + separationDuration;
        }
    }
    
    private void TriggerExplosion()
    {
        if (isExploding) return;
        
        isExploding = true;
        state = State.Exploding;
        rb.velocity = Vector2.zero;
        
        if (animator)
        {
            animator.SetBool("Charge", false);
            animator.SetTrigger("PreBoom");
        }
        
        StartCoroutine(ExplosionSequence());
    }
    
    private IEnumerator ExplosionSequence()
    {
        yield return new WaitForSeconds(0.16f);
        
        if (animator)
        {
            animator.SetTrigger("Boom");
        }
        
        if (audioSource && explosionSfx)
        {
            audioSource.PlayOneShot(explosionSfx);
        }
        
        DealExplosionDamage();
        
        yield return new WaitForSeconds(destroyDelay);
        Destroy(gameObject);
    }
    
    private void DealExplosionDamage()
    {
        Vector2 explosionCenter = transform.position;
        Collider2D[] hits = Physics2D.OverlapCircleAll(explosionCenter, explosionRadius);
        
        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == gameObject) continue;
            
            if (hit.CompareTag("Player"))
            {
                PlayerController pc = hit.GetComponent<PlayerController>();
                if (pc != null)
                {
                    pc.TakeDamage(explosionDamage, explosionCenter);
                }
            }
            
            SlimeEnemyV2 slime = hit.GetComponent<SlimeEnemyV2>();
            if (slime != null)
            {
                slime.TakeDamage(explosionDamage, explosionCenter);
            }
            
            BomberSlime bomber = hit.GetComponent<BomberSlime>();
            if (bomber != null && bomber != this && !bomber.isExploding)
            {
                bomber.TriggerExplosion();
            }
        }
    }
    
    private void UpdateAnimator()
    {
        if (!animator) return;
        
        bool isMoving = rb.velocity.magnitude > 0.1f;
        
        if (HasParameter(animator, "isMoving"))
        {
            animator.SetBool("isMoving", isMoving);
        }
        
        if (Mathf.Abs(rb.velocity.x) > 0.01f)
        {
            Transform spriteTransform = animator.transform;
            spriteTransform.localScale = new Vector3(
                rb.velocity.x > 0 ? 1 : -1,
                1, 1
            );
        }
    }
    
    private bool HasParameter(Animator anim, string paramName)
    {
        foreach (AnimatorControllerParameter param in anim.parameters)
        {
            if (param.name == paramName)
                return true;
        }
        return false;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
        
        if (enablePatrol)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(transform.position, separationDistance);
            
            if (Application.isPlaying)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(patrolTargetA, 0.3f);
                Gizmos.DrawWireSphere(patrolTargetB, 0.3f);
                Gizmos.DrawLine(patrolTargetA, patrolTargetB);
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(currentPatrolTarget, 0.4f);
                Gizmos.DrawLine(transform.position, currentPatrolTarget);
            }
            else
            {
                Vector2 spawnPreview = transform.position;
                Vector2 targetA = spawnPreview + patrolPointA;
                Vector2 targetB = spawnPreview + patrolPointB;
                
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(targetA, 0.3f);
                Gizmos.DrawWireSphere(targetB, 0.3f);
                Gizmos.DrawLine(targetA, targetB);
            }
        }
        else
        {
            Gizmos.color = Color.cyan;
            if (Application.isPlaying)
            {
                Gizmos.DrawWireCube(spawnPos, Vector3.one * 0.5f);
            }
            else
            {
                Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            }
        }
    }
}