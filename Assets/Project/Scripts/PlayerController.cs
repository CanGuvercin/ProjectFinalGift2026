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

    [Header("Combat")]
    [SerializeField] private Collider2D hitBoxCollider;


    [Header("For Player PosRef")]
    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 upOffset = new Vector2(0f, 0.5f);
    [SerializeField] private Vector2 downOffset = new Vector2(0f, -0.5f);
    [SerializeField] private Vector2 leftOffset = new Vector2(-0.5f, 0f);
    [SerializeField] private Vector2 rightOffset = new Vector2(0.5f, 0f);


    [Header("SFX Settings")]
    [SerializeField] private AudioSource sfxSource;
    // Core sounds
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

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 0.3f;

    [Header("Interaction")]
    [SerializeField] private float interactCooldown = 0.2f;
    [Tooltip("Fallback unlock time if animation event is not set yet.")]
    [SerializeField] private float interactFallbackTime = 1f;

    // old cam v1
    //[Header("Camera Reference")]
    //[SerializeField] private CameraController cameraController;
    [Header("Camera Reference")]
    [SerializeField] private PixelPerfectCameraController cameraController;

    private bool isInvulnerable;


    private bool isDashing;
    private float lastDashTime;
    private Coroutine dashCo;

    private float lastAttackTime;
    private float lastInteractTime;

    private bool isInteracting;

    private Rigidbody2D rb;
    private Animator animator;

    // Input System
    private PlayerInputActions inputActions;
    private Vector2 moveInput;

    // Last direction memory (for idle / attack / interact / dash direction)
    private Vector2 lastMoveDir = Vector2.down;

    private PlayerHitBox playerHitBox; // ← Ekle


    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerInputActions();
        hitBoxCollider.enabled = false;
        currentHealth = maxHealth;

        // PlayerHitBox referansını al
        playerHitBox = hitBoxCollider.GetComponent<PlayerHitBox>();

         // Kamerayı otomatik bul
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

    //=========================== DAMAGE SYSYEM ==========================

    public void TakeDamage(int damage, Vector2 damageSourcePos)
    {
        if (isInvulnerable) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;

        // Direction for damage animation
        Vector2 hitDir = (transform.position - (Vector3)damageSourcePos).normalized;
        lastMoveDir = hitDir;

        animator.SetFloat("moveX", hitDir.x);
        animator.SetFloat("moveY", hitDir.y);

        animator.SetBool("isDamaged", true);

        // ✅ KAMERA SHAKE
        if (cameraController != null)
        cameraController.OnPlayerHurt(damage);

        // SFX
        PlayHurtSfx();

        // Knockback
        rb.velocity = Vector2.zero;
        rb.AddForce(hitDir * knockbackForce, ForceMode2D.Impulse);

        StartCoroutine(DamageRoutine());
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
        // During dash / interact, ignore movement input updates (but we still allow dash coroutine to move rb)
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

        // Actions
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
        // IMPORTANT: Do NOT zero velocity here while dashing,
        // because DashRoutine controls rb.velocity during the dash.
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

    //=========================AUDIO AND SFX =========================

    private void PlaySfx(AudioClip clip, float pitchMin = 0.95f, float pitchMax = 1.05f)
    {
        sfxSource.pitch = Random.Range(pitchMin, pitchMax);
        sfxSource.PlayOneShot(clip);
    }

    public void PlaySwordSwingSfx()
    {
        PlaySfx(swordSwing);
    }

    public void PlaySwordHitSfx()
    {
        PlaySfx(swordHit, 0.98f, 1.02f);
    }

    public void PlayDashSfx()
    {
        PlaySfx(dashSfx, 0.9f, 1.1f);
    }

    public void PlayWalkSfx()
    {
        if (moveInput.sqrMagnitude < 0.01f) return;

        PlaySfx(walkSfx, 0.9f, 1.05f);
    }

    public void PlayHurtSfx()
    {
        PlaySfx(hurtSfx, 0.95f, 1.05f);
    }

    // ================ HITBOX / COMBAT ZONE ===================

    public void EnableHitBox()
    {
         hitBoxCollider.enabled = true;
    
    // Hit flag'ini sıfırla - YENİ SWING BAŞLIYOR!
    if (playerHitBox != null)
        playerHitBox.ResetHitFlag();
        
    Debug.Log("[PLAYER] HitBox ENABLED + Flag reset");
    }

    public void DisableHitBox()
    {
        hitBoxCollider.enabled = false;
    Debug.Log("[PLAYER] HitBox DISABLED");
    }


    // ================= ANIMATOR =================

    private void UpdateAnimator()
    {
        // While dashing/interacting, keep "isMoving" false so Idle/Dash trees behave cleanly
        bool isMoving = !isDashing && !isInteracting && moveInput != Vector2.zero;
        animator.SetBool("isMoving", isMoving);

        Vector2 dir = isMoving ? moveInput : lastMoveDir;

        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);

        // This MUST exist in Animator as a bool parameter
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


    private System.Collections.IEnumerator DashRoutine(Vector2 dir)
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

        UpdateAttackPointPosition();

        animator.SetTrigger("attack");

        // Kamera'ya attack bildir (miss olabilir)
        if (cameraController != null)
        cameraController.OnAttackMiss(); // Önce miss varsay
    }

    private void UpdateAttackPointPosition()
    {
        if (lastMoveDir == Vector2.up)
            attackPoint.localPosition = upOffset;
        else if (lastMoveDir == Vector2.down)
            attackPoint.localPosition = downOffset;
        else if (lastMoveDir == Vector2.left)
            attackPoint.localPosition = leftOffset;
        else if (lastMoveDir == Vector2.right)
            attackPoint.localPosition = rightOffset;
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

        // Fallback unlock (until you wire Animation Event properly)
        CancelInvoke(nameof(EndInteract));
        Invoke(nameof(EndInteract), interactFallbackTime);
    }

    // Called by Animation Event on the LAST frame of Interact (recommended)
    public void EndInteract()
    {
        CancelInvoke(nameof(EndInteract));
        isInteracting = false;
    }

    public void OnSwordHit()
    {
    PlaySwordHitSfx();
    Debug.Log("[PLAYER] Sword HIT sound played!");
    }
}
