using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DummyNPC : MonoBehaviour
{
    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitColorDuration = 0.2f;

    [Header("Health (Optional)")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        currentHealth = maxHealth;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Check if hit by attack hitbox
        if (other.CompareTag("PlayerAttack") || other.gameObject.name.Contains("HitBox"))
        {
            TakeHit();
        }
    }

    private void TakeHit()
    {
        if (showDebugLogs)
        {
            Debug.Log($"[DUMMY] Hit detected at {Time.time}");
            Debug.Log($"[DUMMY] Health: {currentHealth}/{maxHealth}");
        }

        // Visual feedback
        if (spriteRenderer != null)
        {
            CancelInvoke(nameof(ResetColor));
            spriteRenderer.color = hitColor;
            Invoke(nameof(ResetColor), hitColorDuration);
        }

        // Damage (optional)
        currentHealth -= 10;
        if (currentHealth <= 0)
        {
            Debug.Log("[DUMMY] Destroyed!");
            // Optionally destroy or respawn
            // Destroy(gameObject);
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

    // Optional: Draw gizmo to see collider bounds
    private void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}