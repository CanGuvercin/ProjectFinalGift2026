using UnityEngine;

public class GridBounce : MonoBehaviour
{
    [Header("Bounce Settings")]
    [SerializeField] private float bounceForce = 5f;
    [Tooltip("Çarpma anında uygulanacak geri tepme kuvveti")]
    
    [SerializeField] private bool ignorePlayer = true;
    [Tooltip("Player'ı yok say (sadece Enemy'ler için bounce)")]
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = false;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Player'ı yok say
        if (ignorePlayer && collision.gameObject.CompareTag("Player"))
        {
            return;
        }
        
        Rigidbody2D rb = collision.rigidbody;
        if (rb == null) return;
        
        // Çarpışma noktasını al
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 collisionNormal = contact.normal; // Duvara dik vektör
        
        // Bounce force uygula (normal yönünde)
        Vector2 bounce = collisionNormal * bounceForce;
        rb.AddForce(bounce, ForceMode2D.Impulse);
        
        if (showDebugLogs)
        {
            Debug.Log($"[GridBounce] {collision.gameObject.name} bounced! Force: {bounce}");
        }
    }
    
    private void OnCollisionStay2D(Collision2D collision)
    {
        // Obje grid içine girdiyse (collision devam ediyorsa)
        // Sürekli itekle
        
        if (ignorePlayer && collision.gameObject.CompareTag("Player"))
        {
            return;
        }
        
        Rigidbody2D rb = collision.rigidbody;
        if (rb == null) return;
        
        // Çarpışma noktasını al
        ContactPoint2D contact = collision.GetContact(0);
        Vector2 collisionNormal = contact.normal;
        
        // Daha hafif bir sürekli itme
        Vector2 pushForce = collisionNormal * (bounceForce * 0.3f);
        rb.AddForce(pushForce, ForceMode2D.Force);
        
        if (showDebugLogs)
        {
            Debug.Log($"[GridBounce] Pushing {collision.gameObject.name} out of grid");
        }
    }
}