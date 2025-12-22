using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;
    [SerializeField] private float attackCooldown = 0.3f;

    private float lastAttackTime;

    private Rigidbody2D rb;
    private Animator animator;

    // Input System
    private PlayerInputActions inputActions;
    private Vector2 moveInput;

    // Last direction memory
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

    private void ReadInput()
    {
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            lastMoveDir = moveInput.normalized;
        }

        if (inputActions.Player.Attack.triggered)
        {
            TryAttack();
        }
    }

    private void Move()
    {
        rb.velocity = moveInput.normalized * moveSpeed;
    }

    private void UpdateAnimator()
    {
        bool isMoving = moveInput != Vector2.zero;
        animator.SetBool("isMoving", isMoving);

        Vector2 dir = isMoving ? moveInput : lastMoveDir;

        animator.SetFloat("moveX", dir.x);
        animator.SetFloat("moveY", dir.y);
    }

    private void TryAttack()
    {
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Attack yönünü son bakılan yöne kilitle
        animator.SetFloat("moveX", lastMoveDir.x);
        animator.SetFloat("moveY", lastMoveDir.y);

        animator.SetTrigger("attack");
    }
}
