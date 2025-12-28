using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    private PlayerController playerController;
    private CameraController cameraController;
    private bool hasHitThisSwing = false;
    
    private void Start()
    {
        playerController = GetComponentInParent<PlayerController>();
        cameraController = Camera.main?.GetComponent<CameraController>();
        
        Debug.Log($"[HITBOX] Start - PlayerController: {(playerController != null ? "FOUND" : "NULL")}");
        Debug.Log($"[HITBOX] Start - CameraController: {(cameraController != null ? "FOUND" : "NULL")}");
    }
    
    public void ResetHitFlag()
    {
        hasHitThisSwing = false;
        Debug.Log("[HITBOX] Hit flag RESET");
    }
    
    private void OnDisable()
    {
        Debug.Log("[HITBOX] OnDisable called");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[HITBOX] TRIGGER! Hit: {other.gameObject.name}, Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        if (hasHitThisSwing)
        {
            Debug.Log("[HITBOX] Already hit this swing, ignoring");
            return;
        }
        
        // ✅ YENİ: Layer veya Tag ile kontrol et (daha genel)
        bool isEnemy = false;
        
        // 1. SlimeEnemy script var mı?
        SlimeEnemy slime = other.GetComponent<SlimeEnemy>();
        if (slime != null)
        {
            isEnemy = true;
            Debug.Log("[HITBOX] Found SlimeEnemy component");
        }
        
        // 2. DummyNPC script var mı?
        DummyNPC dummy = other.GetComponent<DummyNPC>();
        if (dummy != null)
        {
            isEnemy = true;
            Debug.Log("[HITBOX] Found DummyNPC component");
        }
        
        // 3. "Enemy" tag var mı?
        if (other.CompareTag("Enemy"))
        {
            isEnemy = true;
            Debug.Log("[HITBOX] Has 'Enemy' tag");
        }
        
        // 4. "Enemy" layer'ında mı?
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
        {
            isEnemy = true;
            Debug.Log("[HITBOX] On 'Enemy' layer");
        }
        
        // HIT ONAYLANDI!
        if (isEnemy)
        {
            hasHitThisSwing = true;
            
            Debug.Log("[HITBOX] ✅✅✅ HIT CONFIRMED! ✅✅✅");
            
            if (playerController != null)
            {
                playerController.OnSwordHit();
                Debug.Log("[HITBOX] Called PlayerController.OnSwordHit()");
            }
            
            if (cameraController != null)
            {
                cameraController.OnAttackHit();
                Debug.Log("[HITBOX] Called CameraController.OnAttackHit()");
            }
        }
        else
        {
            Debug.Log("[HITBOX] ❌ NOT an enemy, ignoring");
        }
    }
}