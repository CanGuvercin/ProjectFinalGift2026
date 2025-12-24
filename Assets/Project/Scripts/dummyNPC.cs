using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DummyNPC : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("Debug Settings")]
    [SerializeField] private bool showDebugLogs = true;
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitColorDuration = 0.2f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int currentHealth;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Rigidbody2D rb;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = true;
        }
        
        currentHealth = maxHealth;
    }

    // Player'a hasar vermek için
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (showDebugLogs)
            Debug.Log($"[DUMMY] Collision with: {collision.gameObject.name}, Tag: {collision.gameObject.tag}");

        if (!collision.gameObject.CompareTag("Player")) return;

        // Cooldown kontrolü
        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            if (showDebugLogs)
                Debug.Log($"[DUMMY] Dealing {damage} damage to Player!");
            
            player.TakeDamage(damage, transform.position);
        }
    }

    // Player'ın saldırısından hasar almak için
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (showDebugLogs)
            Debug.Log($"[DUMMY] Trigger with: {other.gameObject.name}, Tag: {other.tag}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");

        // PlayerAttack layer veya HitBox isimli objelerden hasar al
        if (other.gameObject.layer == LayerMask.NameToLayer("PlayerAttack") || 
            other.gameObject.name.Contains("HitBox"))
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

        // Damage
        currentHealth -= 10;
        if (currentHealth <= 0)
        {
            Debug.Log("[DUMMY] Destroyed!");
            Destroy(gameObject);
        }
    }

    private void ResetColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;
    }

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