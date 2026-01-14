using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class DummyNPC : MonoBehaviour
{
    [Header("Combat")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float damageCooldown = 1f;
    private float lastDamageTime;

    [Header("Health")]
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("Visual Feedback")]
    [SerializeField] private Color hitColor = Color.red;
    [SerializeField] private float hitColorDuration = 0.2f;

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

    // Player'a temas edince hasar vermek için
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!collision.gameObject.CompareTag("Player")) return;

        if (Time.time - lastDamageTime < damageCooldown) return;
        lastDamageTime = Time.time;

        PlayerController player = collision.gameObject.GetComponent<PlayerController>();
        if (player != null)
        {
            player.TakeDamage(damage, transform.position);
        }
    }

    // Player saldırısından hasar almak için
    private void OnTriggerEnter2D(Collider2D other)
    {
        bool isPlayerAttack =
            other.gameObject.layer == LayerMask.NameToLayer("PlayerAttack");

        bool isHitBox =
            other.gameObject.name.Contains("HitBox");

        if (isPlayerAttack || isHitBox)
        {
            TakeHit();
        }
    }

    private void TakeHit()
    {
        // Visual feedback
        if (spriteRenderer != null)
        {
            CancelInvoke(nameof(ResetColor));
            spriteRenderer.color = hitColor;
            Invoke(nameof(ResetColor), hitColorDuration);
        }

        currentHealth -= 10;

        if (currentHealth <= 0)
        {
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
