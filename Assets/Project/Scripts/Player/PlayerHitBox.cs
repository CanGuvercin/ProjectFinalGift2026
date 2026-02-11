using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    [SerializeField] private int damage = 20;
    
    private PlayerController playerController;
    private PixelPerfectCameraController cameraController;
    private bool hasHitThisSwing = false;
    
    private void Awake()
    {
        playerController = GetComponentInParent<PlayerController>();
        cameraController = Camera.main?.GetComponent<PixelPerfectCameraController>();
        
        Debug.Log($"[HITBOX INIT] {gameObject.name} initialized - Damage: {damage}");
    }
    
    public void ResetHitFlag()
    {
        hasHitThisSwing = false;
        Debug.Log($"[HITBOX] {gameObject.name} - Hit flag reset");
    }
    
    private void OnEnable()
    {
        hasHitThisSwing = false;
        Debug.Log($"[HITBOX ENABLE] {gameObject.name} activated!");
    }
    
    private void OnDisable()
    {
        Debug.Log($"[HITBOX DISABLE] {gameObject.name} deactivated");
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[HITBOX TRIGGER] {gameObject.name} hit: '{other.name}' | Tag: {other.tag} | Layer: {LayerMask.LayerToName(other.gameObject.layer)}");
        
        if (hasHitThisSwing)
        {
            Debug.Log($"[HITBOX] Already hit this swing, ignoring");
            return;
        }
        
        // ═══ SlimeEnemyV2 (YENİ!) ═══
        SlimeEnemyV2 slimeV2 = other.GetComponent<SlimeEnemyV2>();
        if (slimeV2 != null)
        {
            Debug.Log($"[HITBOX] ►►► SlimeEnemyV2 FOUND! Dealing {damage} damage ◄◄◄");
            slimeV2.TakeDamage(damage, transform.position);
            hasHitThisSwing = true;
            
            if (playerController != null)
                playerController.OnSwordHit();
            
            if (cameraController != null)
                cameraController.OnAttackHit();
            
            return;
        }
        
        // ═══ SlimeEnemy (ESKİ) ═══
        SlimeEnemy slimeOld = other.GetComponent<SlimeEnemy>();
        if (slimeOld != null)
        {
            Debug.Log($"[HITBOX] ✓ Old SlimeEnemy found");
            hasHitThisSwing = true;
            
            if (playerController != null)
                playerController.OnSwordHit();
            
            if (cameraController != null)
                cameraController.OnAttackHit();
            
            return;
        }
        
        // ═══ DummyNPC ═══
        DummyNPC dummy = other.GetComponent<DummyNPC>();
        if (dummy != null)
        {
            Debug.Log($"[HITBOX] ✓ DummyNPC found");
            hasHitThisSwing = true;
            
            if (playerController != null)
                playerController.OnSwordHit();
            
            if (cameraController != null)
                cameraController.OnAttackHit();
            
            return;
        }
        
        // ═══ Tag/Layer Fallback ═══
        bool isEnemy = other.CompareTag("Enemy") || other.gameObject.layer == LayerMask.NameToLayer("Enemy");
        
        if (isEnemy)
        {
            Debug.Log($"[HITBOX] ✓ Enemy detected by Tag/Layer (no specific component)");
            hasHitThisSwing = true;
            
            if (playerController != null)
                playerController.OnSwordHit();
            
            if (cameraController != null)
                cameraController.OnAttackHit();
        }
        else
        {
            Debug.Log($"[HITBOX] ✗ Not an enemy: {other.name}");
        }
    }
}