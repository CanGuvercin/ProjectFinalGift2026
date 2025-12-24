using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
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

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerInputActions();
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

        rb.velocity = moveInput.normalized * moveSpeed;
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
        Debug.Log("isDashing: " + isDashing);
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
        animator.SetTrigger("attack");
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
}
