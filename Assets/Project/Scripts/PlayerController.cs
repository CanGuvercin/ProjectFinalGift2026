using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3.5f;

    private Rigidbody2D rb;
    private Animator animator;

    // Our Input System
    private PlayerInputActions InputActions;
    private Vector2 moveInput;

    // Direction Memory for last point
    private Vector2 lastMoveDir = Vector2.down;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();

        InputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        InputActions.Enable();
    }

    private void OnDisable()
    {
        InputActions.Disable();
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

    private void ReadInput ()
    {
        moveInput = InputActions.Player.Move.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            lastMoveDir = moveInput.normalized;
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
        Vector2 dir= isMoving ? moveInput : lastMoveDir;  
        // if there is motion, keep input, 
        // if there is no last direction will decide for last sprite/animation

        animator.SetFloat("moveX",dir.x);
        animator.SetFloat("moveY", dir.y);

    }






}
