using System.Collections;
using System.Collections.Generic;
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

    [Header("Perry System")]
    [SerializeField] private float perryActivationDelay = 0.05f;
    [SerializeField] private float perryActiveDuration = 0.15f;
    [SerializeField] private float perryCooldown = 2f;
    [SerializeField] private GameObject bulletPlusPrefab;
    [SerializeField] private float bulletReflectSpeed = 10f;
    [SerializeField] private CapsuleCollider2D playerBodyCollider;

    [Header("SFX Settings")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip swordSwing;
    [SerializeField] private AudioClip swordHit;
    [SerializeField] private AudioClip dashSfx;
    [SerializeField] private AudioClip walkSfx;
    [SerializeField] private AudioClip hurtSfx;
    [SerializeField] private AudioClip perryDeflectSfx;

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
    
    private bool attackHitSomething;

    // Perry state
    private bool isPerryActive = false;
    private bool canPerry = true;
    private GameObject activePerryHitbox = null;
    private HashSet<Collider2D> detectedBulletsInPerryZone = new HashSet<Collider2D>();
    private HashSet<Collider2D> alreadyDeflectedBullets = new HashSet<Collider2D>();

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private PlayerInputActions inputActions;
    private Vector2 moveInput;
    private Vector2 lastMoveDir = Vector2.down;

    private int attackCounter = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        inputActions = new PlayerInputActions();
        
        DisableAllHitBoxes();
        
        LoadHealth();

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
        // Pause'dayken input okuma
    if (Time.timeScale == 0f) return;
        ReadInput();
        UpdateAnimator();
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0f) return;
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

    // ================= HEALTH SAVE/LOAD SYSTEM =================
    
    private void LoadHealth()
    {
        if (PlayerPrefs.HasKey("PlayerCurrentHP"))
        {
            currentHealth = PlayerPrefs.GetInt("PlayerCurrentHP");
        }
        else
        {
            currentHealth = maxHealth;
            SaveHealth();
        }
        
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    }
    
    private void SaveHealth()
    {
        PlayerPrefs.SetInt("PlayerCurrentHP", currentHealth);
        PlayerPrefs.Save();
    }
    
    public void ResetHealthToMax()
    {
        currentHealth = maxHealth;
        SaveHealth();
    }

    // ================= DAMAGE SYSTEM =================
    
    public void TakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (isInvulnerable || isPerryActive) return;

        float difficultyMultiplier = GameplayManager.Instance != null 
            ? GameplayManager.Instance.GetIncomingDamageMultiplier() 
            : 1.0f;
        
        int finalDamage = Mathf.RoundToInt(damage * difficultyMultiplier);

        currentHealth -= finalDamage;
        if (currentHealth < 0) currentHealth = 0;
        
        SaveHealth();

        Vector2 hitDir = (transform.position - (Vector3)damageSourcePos).normalized;
        lastMoveDir = hitDir;

        if (currentHealth <= 0)
        {
            int currentState = PlayerPrefs.GetInt("GameState", 1);
            
            bool isImmortal = System.Array.Exists(immortalStates, state => state == currentState);
            
            if (isImmortal)
            {
                currentHealth = 1;
                SaveHealth();
                
                animator.SetFloat("moveX", hitDir.x);
                animator.SetFloat("moveY", hitDir.y);
                animator.SetBool("isDamaged", true);
                
                if (cameraController != null)
                    cameraController.OnPlayerHurt(finalDamage);
                
                PlayHurtSfx();
                
                rb.velocity = Vector2.zero;
                rb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);
                
                StartCoroutine(DamageRoutine());
                return;
            }
            OnPlayerDeath();
            return;
        }

        animator.SetFloat("moveX", hitDir.x);
        animator.SetFloat("moveY", hitDir.y);
        animator.SetBool("isDamaged", true);

        if (cameraController != null)
            cameraController.OnPlayerHurt(finalDamage);

        PlayHurtSfx();

        rb.velocity = Vector2.zero;
        rb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(DamageRoutine());
    }
    
    private void OnPlayerDeath()
    {
        enabled = false;
        
        if (animator != null)
        {
            animator.SetTrigger("Death");
        }
        
        ResetHealthToMax();
        
        if (GameOverManager.Instance != null)
        {
            GameOverManager.Instance.ShowGameOver();
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
        if (isInteracting || isDashing || isPerryActive)
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
        if (inputActions.Player.Perry.triggered)
            TryPerry();
    }

    // ================= MOVEMENT =================

    private void Move()
    {
        if (isInteracting || isPerryActive)
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

    public void PlayPerryDeflectSfx() => PlaySfx(perryDeflectSfx, 0.95f, 1.05f);

    // ================= HITBOX / COMBAT ZONE =================

    public void EnableHitBox()
    {
        attackHitSomething = false;
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
        if (!attackHitSomething && cameraController != null)
        {
            cameraController.OnAttackMiss();
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
        bool isMoving = !isDashing && !isInteracting && !isPerryActive && moveInput != Vector2.zero;
        animator.SetBool("isMoving", isMoving);

        Vector2 dir = isMoving ? moveInput : lastMoveDir;
        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);
        animator.SetBool("isDashing", isDashing);
    }

    // ================= DASH =================

    private void TryDash()
    {
        if (isInteracting || isDashing || isPerryActive) return;
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

    // ================= PERRY SYSTEM =================

    private void TryPerry()
    {
        if (isInteracting || isDashing || isPerryActive) return;
        if (!canPerry) return;

        StartCoroutine(PerryCoroutine());
    }

    private IEnumerator PerryCoroutine()
    {
        canPerry = false;
        isPerryActive = false;

        Vector2 directionBeforePerry = lastMoveDir;

        rb.velocity = Vector2.zero;

        animator.SetTrigger("Perry");
        animator.SetFloat("moveX", lastMoveDir.x);
        animator.SetFloat("moveY", lastMoveDir.y);

        activePerryHitbox = GetActivePerryHitbox(lastMoveDir);
        
        if (activePerryHitbox != null)
        {
            activePerryHitbox.SetActive(true);
        }

        yield return new WaitForSeconds(perryActivationDelay);

        isPerryActive = true;
        detectedBulletsInPerryZone.Clear();
        alreadyDeflectedBullets.Clear();

        yield return new WaitForSeconds(perryActiveDuration);

        isPerryActive = false;
        
        if (activePerryHitbox != null)
        {
            activePerryHitbox.SetActive(false);
        }
        
        activePerryHitbox = null;
        detectedBulletsInPerryZone.Clear();
        alreadyDeflectedBullets.Clear();

        lastMoveDir = directionBeforePerry;
        animator.SetFloat("moveX", lastMoveDir.x);
        animator.SetFloat("moveY", lastMoveDir.y);

        yield return new WaitForSeconds(perryCooldown);
        canPerry = true;
    }

    private GameObject GetActivePerryHitbox(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
        {
            return direction.x > 0 ? hitBoxRight : hitBoxLeft;
        }
        else
        {
            return direction.y > 0 ? hitBoxUp : hitBoxDown;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
{
    // Sadece ismi "Bullet" veya "Projectile" içeren objeleri kabul et
    bool isBullet = (collision.CompareTag("Enemy") || collision.CompareTag("Bullet")) 
                    && (collision.gameObject.name.Contains("Bullet") || collision.gameObject.name.Contains("Projectile"));
    
    if (isPerryActive && isBullet)
    {
        if (IsColliderInActivePerryZone(collision))
        {
            detectedBulletsInPerryZone.Add(collision);
        }
    }
}

private void OnTriggerStay2D(Collider2D collision)
{
    if (!isPerryActive || activePerryHitbox == null) return;

    // Sadece ismi "Bullet" veya "Projectile" içeren objeleri kabul et
    bool isBullet = (collision.CompareTag("Enemy") || collision.CompareTag("Bullet")) 
                    && (collision.gameObject.name.Contains("Bullet") || collision.gameObject.name.Contains("Projectile"));
    
    if (isBullet)
    {
        if (alreadyDeflectedBullets.Contains(collision))
        {
            return;
        }
        
        if (detectedBulletsInPerryZone.Contains(collision))
        {
            if (!IsBulletTouchingPlayerBody(collision))
            {
                alreadyDeflectedBullets.Add(collision);
                DeflectBullet(collision);
                detectedBulletsInPerryZone.Remove(collision);
            }
        }
    }
}
  
private void OnTriggerExit2D(Collider2D collision)
{
    // Sadece ismi "Bullet" veya "Projectile" içeren objeleri kabul et
    bool isBullet = (collision.CompareTag("Enemy") || collision.CompareTag("Bullet")) 
                    && (collision.gameObject.name.Contains("Bullet") || collision.gameObject.name.Contains("Projectile"));
    
    if (isBullet)
    {
        detectedBulletsInPerryZone.Remove(collision);
    }
}
    private bool IsColliderInActivePerryZone(Collider2D bullet)
    {
        if (activePerryHitbox == null) return false;

        Collider2D hitboxCollider = activePerryHitbox.GetComponent<Collider2D>();
        if (hitboxCollider == null) return false;

        return hitboxCollider.IsTouching(bullet);
    }

    private bool IsBulletTouchingPlayerBody(Collider2D bullet)
    {
        if (playerBodyCollider == null) return false;
        return playerBodyCollider.IsTouching(bullet);
    }

    private void DeflectBullet(Collider2D bulletCollider)
{
    Rigidbody2D bulletRb = bulletCollider.GetComponent<Rigidbody2D>();
    if (bulletRb == null) return;

    Vector2 incomingDirection = bulletRb.velocity.normalized;
    Vector2 reflectDirection = -incomingDirection;

    // Bullet'ı yok et
    Destroy(bulletCollider.gameObject);

    if (bulletPlusPrefab == null) return;

    // Player'ın capsule collider merkezinden spawn et
    Vector3 spawnPos = playerBodyCollider != null 
        ? playerBodyCollider.bounds.center 
        : transform.position;

    GameObject reflectedBullet = Instantiate(bulletPlusPrefab, spawnPos, Quaternion.identity);
    
    Rigidbody2D reflectedRb = reflectedBullet.GetComponent<Rigidbody2D>();
    if (reflectedRb != null)
    {
        reflectedRb.velocity = reflectDirection * bulletReflectSpeed;
    }

    // Perry deflect SFX çal
    PlayPerryDeflectSfx();
}
//
    // ================= ATTACK =================

    private void TryAttack()
    {
        if (isInteracting || isDashing || isPerryActive) return;
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
        EnableHitBox();
        
        yield return new WaitForSeconds(0.2f);
        DisableHitBox();
        
        yield return new WaitForSeconds(0.05f);
        ApplyAtomicNudge();
    }

    private void ApplyAtomicNudge()
    {
        attackCounter++;
        
        float nudgeAmount = 0.0001f;
        Vector2 nudgeDirection;
        
        if (attackCounter % 4 == 1)
        {
            nudgeDirection = Vector2.right;
        }
        else if (attackCounter % 4 == 2)
        {
            nudgeDirection = Vector2.left;
        }
        else if (attackCounter % 4 == 3)
        {
            nudgeDirection = Vector2.right;
        }
        else
        {
            nudgeDirection = Vector2.left;
        }
        
        Vector3 newPos = transform.position + (Vector3)(nudgeDirection * nudgeAmount);
        transform.position = newPos;
    }

    // ================= INTERACT =================

    private void TryInteract()
    {
        if (isInteracting || isDashing || isPerryActive) return;
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
        attackHitSomething = true;
        PlaySwordHitSfx();
    }
    
    public void MarkAttackHit()
    {
        attackHitSomething = true;
    }

    public void Heal(int amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Min(maxHealth, currentHealth);
        SaveHealth();
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

    public void FreezePlayer()
    {
        enabled = false;
        rb.velocity = Vector2.zero;
        rb.angularVelocity = 0f;
        moveInput = Vector2.zero;
    }
    
    public void UnfreezePlayer()
    {
        enabled = true;
    }
}