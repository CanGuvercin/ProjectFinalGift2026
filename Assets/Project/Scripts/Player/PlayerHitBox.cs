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
    }
    
    public void ResetHitFlag()
    {
        hasHitThisSwing = false;
        Debug.Log("[PlayerHitBox] ✅ ResetHitFlag called");
    }
    
    private void OnEnable()
    {
        hasHitThisSwing = false;
        Debug.Log("[PlayerHitBox] ⚡ OnEnable - hasHitThisSwing RESET to false");
    }
    
    private void OnDisable()
    {
        Debug.Log($"[PlayerHitBox] 💤 OnDisable - hasHitThisSwing was: {hasHitThisSwing}");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[PlayerHitBox] 🎯 OnTriggerEnter2D: {other.gameObject.name}, hasHitThisSwing: {hasHitThisSwing}");
        
        if (hasHitThisSwing)
        {
            Debug.Log("[PlayerHitBox] ⏭️ Already hit this swing, skipping");
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
            Debug.Log("[PlayerHitBox] ✅ Enemy detected! Setting hasHitThisSwing = true");
            
            if (playerController != null)
            {
                playerController.OnSwordHit();
            }
            
            if (cameraController != null)
            {
                cameraController.OnAttackHit();
            }
        }
        else
        {
            Debug.Log($"[PlayerHitBox] ❌ Not an enemy: {other.gameObject.name}");
        }
    }
}