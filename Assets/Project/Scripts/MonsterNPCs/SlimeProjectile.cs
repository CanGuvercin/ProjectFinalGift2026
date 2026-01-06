using UnityEngine;

public class SlimeProjectile : MonoBehaviour
{
    [SerializeField] private int damage = 5;
    [SerializeField] private float lifetime = 5f;
    
    private void Start()
    {
        Destroy(gameObject, lifetime);
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
                // Player'a çarp
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.TakeDamage(damage, transform.position);

                CameraController cam = Camera.main?.GetComponent<CameraController>();
                   if (cam != null)
                    cam.OnPlayerHurt(damage);

            }
            Destroy(gameObject);
        }
        
        // Duvara çarp
        if (other.gameObject.layer == LayerMask.NameToLayer("Obstacle") || 
            other.gameObject.layer == LayerMask.NameToLayer("Default"))
        {
            Destroy(gameObject);
        }
    }
}