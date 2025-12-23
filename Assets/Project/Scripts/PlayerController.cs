using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3.5f;

    [Header("Combat")]
    [SerializeField] private float attackCooldown = 0.3f;

    [Header("Interaction")]
    [SerializeField] private float interactCooldown = 0.2f;

    private float lastAttackTime;
    private float lastInteractTime;

    private bool isInteracting;

    private Rigidbody2D rb;
    private Animator animator;

    // Input System
    private PlayerInputActions inputActions;
    private Vector2 moveInput;

    // Last direction memory (for idle / attack / interact direction)
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
        // HARD LOCK during interaction
        if (isInteracting)
        {
            moveInput = Vector2.zero;
            return;
        }

        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            lastMoveDir = moveInput.normalized;
        }

        if (inputActions.Player.Attack.triggered)
        {
            TryAttack();
        }

        if (inputActions.Player.Interact.triggered)
        {
            TryInteract();
        }
    }

    // ================= MOVEMENT =================

    private void Move()
    {
        if (isInteracting)
        {
            rb.velocity = Vector2.zero;
            return;
        }

        rb.velocity = moveInput.normalized * moveSpeed;
    }

    // ================= ANIMATOR =================

    private void UpdateAnimator()
    {
        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("isMoving", isMoving);

        Vector2 dir = isMoving ? moveInput : lastMoveDir;

        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);
    }

    // ================= ATTACK =================

    private void TryAttack()
    {
        if (isInteracting) return;
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
        if (isInteracting) return;
        if (Time.time - lastInteractTime < interactCooldown) return;

        isInteracting = true;
        lastInteractTime = Time.time;

        rb.velocity = Vector2.zero;

        animator.SetFloat("moveX", lastMoveDir.x);
        animator.SetFloat("moveY", lastMoveDir.y);
        animator.SetTrigger("Interact");

        CancelInvoke(nameof(EndInteract));
        Invoke(nameof(EndInteract), 1f); // fallback only
    }

    public void EndInteract()
    {
        CancelInvoke(nameof(EndInteract));
        isInteracting = false;
    }

}
