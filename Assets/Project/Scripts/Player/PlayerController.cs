using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;
    [SerializeField] private float invulnerableTime = 0.6f;
    [SerializeField] private float knockbackForce = 3f;

    [Header("Death Settings")]
    [SerializeField] private int[] immortalStates = { 0, 1 };

    [Header("Combat - Directional HitBoxes")]
    [SerializeField] private GameObject hitBoxRight;
    [SerializeField] private GameObject hitBoxLeft;
    [SerializeField] private GameObject hitBoxUp;
    [SerializeField] private GameObject hitBoxDown;

    [Header("SFX Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip swordSwing;
    [SerializeField] private AudioClip swordHit;
    [SerializeField] private AudioClip dashSfx;
    [SerializeField] private AudioClip walkSfx;
    [SerializeField] private AudioClip hurtSfx;

    [Header("Dash")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.35f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Combat Cooldown")]
    [SerializeField] private float attackCooldown = 0.3f;

    [Header("Interaction")]
    [SerializeField] private float interactCooldown = 0.2f;
    [SerializeField] private float interactFallbackTime = 1f;

    [Header("Camera Reference")]
    [SerializeField] private PixelPerfectCameraController cameraController;

    [Header("Rendering Fix")]
    [SerializeField] private int forcedSortingOrder = 0;

    [Header("VFX")]
    [SerializeField] private Animator slashVFXAnimator;

    private bool isInvulnerable;
    private bool isDashing;
    private float lastDashTime;
    private Coroutine dashCo;
    private float lastAttackTime;
    private float lastInteractTime;
    private bool isInteracting;
    
    // Attack hit tracking
    private bool attackHitSomething;

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.down;

    //for our disaster solution "atomic movement"
    private int attackCounter = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        inputActions = new PlayerInputActions();
        
        DisableAllHitBoxes();
        
        currentHealth = 50;

        if (cameraController == null)
            cameraController = Camera.main.GetComponent<PixelPerfectCameraController>();
    }

    private void OnEnable()
    {
        inputActions.Enable();
    }

    private void OnDisable()
    {
        inputActions.Disable();
    }

    private void Update()
    {
        ReadInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        Move();
    }

    private void LateUpdate()
    {
        Vector3 pos = transform.position;
        if (Mathf.Abs(pos.z) > 0.001f)
        {
            pos.z = 0f;
            transform.position = pos;
        }

        if (spriteRenderer != null && spriteRenderer.sortingOrder != forcedSortingOrder)
        {
            spriteRenderer.sortingOrder = forcedSortingOrder;
        }
    }

    // ================= DAMAGE SYSTEM =================

    public void TakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (isInvulnerable) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        Vector2 hitDir = (transform.position - (Vector3)damageSourcePos).normalized;
        lastMoveDir = hitDir;

        if (currentHealth <= 0)
        {
            Debug.Log("[Player] HP = 0!");
            
            int currentState = PlayerPrefs.GetInt("GameState", 1);
            
            bool isImmortal = System.Array.Exists(immortalStates, state => state == currentState);
            
            if (isImmortal)
            {
                Debug.Log($"[Player] State {currentState} is immortal - HP clamped to 1!");
                currentHealth = 1;
                
                animator.SetFloat("moveX", hitDir.x);
                animator.SetFloat("moveY", hitDir.y);
                animator.SetBool("isDamaged", true);
                
                if (cameraController != null)
                    cameraController.OnPlayerHurt(damage);
                
                PlayHurtSfx();
                
                rb.velocity = Vector2.zero;
                rb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);
                
                StartCoroutine(DamageRoutine());
                return;
            }
            
            Debug.Log("[Player] Triggering Game Over...");
            OnPlayerDeath();
            return;
        }

        animator.SetFloat("moveX", hitDir.x);
        animator.SetFloat("moveY", hitDir.y);
        animator.SetBool("isDamaged", true);

        if (cameraController != null)
            cameraController.OnPlayerHurt(damage);

        PlayHurtSfx();

        rb.velocity = Vector2.zero;
        rb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(DamageRoutine());
    }
    
    private void OnPlayerDeath()
    {
        Debug.Log("[Player] Player died!");
        
        enabled = false;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
        }
        else
        {
            Debug.LogError("[Player] GameOverManager not found!");
        }
    }

    private IEnumerator DamageRoutine()
    {
        isInvulnerable = true;
        yield return new WaitForSeconds(0.1f);
        animator.SetBool("isDamaged", false);
        yield return new WaitForSeconds(invulnerableTime);
        isInvulnerable = false;
    }

    public void TakeDamage()
    {
        animator.SetTrigger("Damage");
        PlayHurtSfx();
    }

    // ================= INPUT =================

    private void ReadInput()
    {
        if (isInteracting || isDashing)
        {
            moveInput = Vector2.zero;
        }
        else
        {
            moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            if (moveInput != Vector2.zero)
                lastMoveDir = moveInput.normalized;
        }

        if (inputActions.Player.Attack.triggered)
            TryAttack();
        if (inputActions.Player.Interact.triggered)
            TryInteract();
        if (inputActions.Player.Dash.triggered)
            TryDash();
    }

    // ================= MOVEMENT =================

    private void Move()
    {
        if (isInteracting)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        if (isDashing)
            return;

        Vector2 velocity = moveInput.normalized * moveSpeed;
        rb.velocity = velocity;
    }

    // ================= AUDIO AND SFX =================

    private void PlaySfx(AudioClip clip, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySwordSwingSfx() => PlaySfx(swordSwing);
    public void PlaySwordHitSfx() => PlaySfx(swordHit, 0.98f, 1.02f);
    public void PlayDashSfx() => PlaySfx(dashSfx, 0.9f, 1.1f);
    public void PlayWalkSfx()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;
        PlaySfx(walkSfx, 0.9f, 1.05f);
    }
    public void PlayHurtSfx() => PlaySfx(hurtSfx, 0.95f, 1.05f);

    // ================= HITBOX / COMBAT ZONE =================

    public void EnableHitBox()
    {
        // Reset hit tracking
        attackHitSomething = false;
        
        Debug.Log("[PlayerController] 🎯 EnableHitBox - attackHitSomething RESET to false");
        
        DisableAllHitBoxes();
        
        if (Mathf.Abs(lastMoveDir.x) > Mathf.Abs(lastMoveDir.y))
        {
            if (lastMoveDir.x > 0)
            {
                if (hitBoxRight != null)
                {
                    hitBoxRight.SetActive(true);
                }
            }
            else
            {
                if (hitBoxLeft != null)
                {
                    hitBoxLeft.SetActive(true);
                }
            }
        }
        else
        {
            if (lastMoveDir.y > 0)
            {
                if (hitBoxUp != null)
                {
                    hitBoxUp.SetActive(true);
                }
            }
            else
            {
                if (hitBoxDown != null)
                {
                    hitBoxDown.SetActive(true);
                }
            }
        }
    }

    public void DisableHitBox()
    {
        DisableAllHitBoxes();
        
        Debug.Log($"[PlayerController] 🔍 DisableHitBox - attackHitSomething: {attackHitSomething}");
        
        // Hitbox kapanırken hiçbir şeye değmediyse miss SFX çal
        if (!attackHitSomething && cameraController != null)
        {
            Debug.Log("[PlayerController] ❌ Playing MISS SFX (no hit detected)");
            cameraController.OnAttackMiss();
        }
        else
        {
            Debug.Log("[PlayerController] ✅ Skipping MISS SFX (hit something!)");
        }
    }
    
    private void DisableAllHitBoxes()
    {
        hitBoxRight?.SetActive(false);
        hitBoxLeft?.SetActive(false);
        hitBoxUp?.SetActive(false);
        hitBoxDown?.SetActive(false);
    }

    // ================= ANIMATOR =================

    private void UpdateAnimator()
    {
        bool isMoving = !isDashing && !isInteracting && moveInput != Vector2.zero;
        animator.SetBool("isMoving", isMoving);

        Vector2 dir = isMoving ? moveInput : lastMoveDir;
        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);
        animator.SetBool("isDashing", isDashing);
    }

    // ================= DASH =================

    private void TryDash()
    {
        if (isInteracting || isDashing) return;
        if (Time.time - lastDashTime < dashCooldown) return;

        lastDashTime = Time.time;
        Vector2 dir = (moveInput != Vector2.zero) ? moveInput.normalized : lastMoveDir;

        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);

        isDashing = true;
        animator.SetBool("isDashing", true);

        StartCoroutine(DashRoutine(dir));
    }

    private IEnumerator DashRoutine(Vector2 dir)
    {
        float t = 0f;

        while (t < dashDuration)
        {
            rb.velocity = dir * dashSpeed;
            t += Time.deltaTime;
            yield return null;
        }

        rb.velocity = Vector2.zero;
        isDashing = false;
        animator.SetBool("isDashing", false);
    }

    // ================= ATTACK =================

    private void TryAttack()
{
    if (isInteracting || isDashing) return;
    if (Time.time - lastAttackTime < attackCooldown) return;

    lastAttackTime = Time.time;
    rb.velocity = Vector2.zero;

    animator.SetFloat("moveX", lastMoveDir.x);
    animator.SetFloat("moveY", lastMoveDir.y);

    if (slashVFXAnimator != null)
    {
        slashVFXAnimator.SetFloat("moveX", lastMoveDir.x);
        slashVFXAnimator.SetFloat("moveY", lastMoveDir.y);
    }

    animator.SetTrigger("attack");
    
    StartCoroutine(AttackHitboxSequence());

    PlaySlashVFX();
}
    
    private IEnumerator AttackHitboxSequence()
{
    yield return new WaitForSeconds(0.1f);
    
    Debug.Log("[PlayerController] 🗡️ Manually enabling hitbox");
    EnableHitBox();
    
    yield return new WaitForSeconds(0.2f);
    
    Debug.Log("[PlayerController] 🛡️ Manually disabling hitbox");
    DisableHitBox();
    
    // ATOMIK İTME SİSTEMİ - Mustafa Can'ın gizli silahı! 🚀
    yield return new WaitForSeconds(0.05f);
    ApplyAtomicNudge();
}

//disaster solution - atomic movement for sec hit box problem
private void ApplyAtomicNudge()
{
    // Vitesli sistem: sağ-sol-sağ-sol
    attackCounter++;
    
    float nudgeAmount = 0.0001f; // Atomik seviye
    Vector2 nudgeDirection;
    
    if (attackCounter % 4 == 1)
    {
        nudgeDirection = Vector2.right; // 1. vuruş: sağa
        Debug.Log("[PlayerController] ⚛️ Atomic nudge: RIGHT");
    }
    else if (attackCounter % 4 == 2)
    {
        nudgeDirection = Vector2.left; // 2. vuruş: sola
        Debug.Log("[PlayerController] ⚛️ Atomic nudge: LEFT");
    }
    else if (attackCounter % 4 == 3)
    {
        nudgeDirection = Vector2.right; // 3. vuruş: sağa
        Debug.Log("[PlayerController] ⚛️ Atomic nudge: RIGHT");
    }
    else
    {
        nudgeDirection = Vector2.left; // 4. vuruş: sola
        Debug.Log("[PlayerController] ⚛️ Atomic nudge: LEFT");
    }
    
    // Atomik hareket uygula
    Vector3 newPos = transform.position + (Vector3)(nudgeDirection * nudgeAmount);
    transform.position = newPos;
    
    Debug.Log($"[PlayerController] 🔬 Atomic nudge applied: {nudgeDirection * nudgeAmount}, Counter: {attackCounter}");
}


    // ================= INTERACT =================

    private void TryInteract()
    {
        if (isInteracting || isDashing) return;
        if (Time.time - lastInteractTime < interactCooldown) return;

        isInteracting = true;
        lastInteractTime = Time.time;
        rb.velocity = Vector2.zero;

        animator.SetFloat("moveX", lastMoveDir.x);
        animator.SetFloat("moveY", lastMoveDir.y);
        animator.SetTrigger("Interact");

        CancelInvoke(nameof(EndInteract));
        Invoke(nameof(EndInteract), interactFallbackTime);
    }

    public void EndInteract()
    {
        CancelInvoke(nameof(EndInteract));
        isInteracting = false;
    }

    public void OnSwordHit()
    {
        Debug.Log("[PlayerController] ⚔️ OnSwordHit called! Setting attackHitSomething = true");
        // HitBox bir şeye değdi!
        attackHitSomething = true;
        PlaySwordHitSfx();
    }
    
    // PlayerHitBox'tan da erişilebilir olması için
    public void MarkAttackHit()
    {
        attackHitSomething = true;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
    }

    public int GetMaxHealth()
    {
        return maxHealth;
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    // ================= VFX =================

    public void PlaySlashVFX()
    {
        if (slashVFXAnimator == null)
        {
            return;
        }
        
        AnimatorStateInfo stateInfo = slashVFXAnimator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("SlashBlendTree") && stateInfo.normalizedTime < 0.9f)
        {
            return;
        }
        
        slashVFXAnimator.SetFloat("moveX", lastMoveDir.x);
        slashVFXAnimator.SetFloat("moveY", lastMoveDir.y);
        
        slashVFXAnimator.SetTrigger("PlaySlash");
    }
}