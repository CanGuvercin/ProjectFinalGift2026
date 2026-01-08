using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    private PlayerController playerController;
    private PixelPerfectCameraController cameraController;
    private bool hasHitThisSwing = false;
    
    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        cameraController = Camera.main?.GetComponent<PixelPerfectCameraController>();
        
        Debug.Log($"[HITBOX] {gameObject.name} Awake - PlayerController: {(playerController != null ? "FOUND" : "NULL")}");
    }
    
    public void ResetHitFlag()
    {
        hasHitThisSwing = false;
        Debug.Log($"[HITBOX] {gameObject.name} 🔄 Flag RESET to FALSE");
    }
    
    private void OnEnable()
    {
        hasHitThisSwing = false;
        Debug.Log($"[HITBOX] {gameObject.name} ✅ OnEnable - Flag = FALSE");
    }
    
    private void OnDisable()
    {
        Debug.Log($"[HITBOX] {gameObject.name} ❌ OnDisable");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[HITBOX] {gameObject.name} 💥 TRIGGER! Target: {other.gameObject.name}, CurrentFlag: {hasHitThisSwing}");
        
        if (hasHitThisSwing)
        {
            Debug.LogWarning($"[HITBOX] {gameObject.name} ⛔ BLOCKED! Already hit this swing!");
            return;
        }
        
        // Enemy kontrolü
        bool isEnemy = false;
        
        if (other.GetComponent<SlimeEnemy>() != null) isEnemy = true;
        if (other.GetComponent<DummyNPC>() != null) isEnemy = true;
        if (other.CompareTag("Enemy")) isEnemy = true;
        if (other.gameObject.layer == LayerMask.NameToLayer("Enemy")) isEnemy = true;
        
        if (isEnemy)
        {
            hasHitThisSwing = true;
            
            Debug.Log($"[HITBOX] {gameObject.name} ✅✅✅ HIT CONFIRMED! Flag → TRUE");
            
            if (playerController != null)
            {
                playerController.OnSwordHit();
                playerController.PlaySlashVFX();
            }
            
            if (cameraController != null)
            {
                cameraController.OnAttackHit();
            }
        }
        else
        {
            Debug.Log($"[HITBOX] {gameObject.name} ❌ NOT an enemy");
        }
    }
}