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
    }
    
    private void OnEnable()
    {
        hasHitThisSwing = false;
    }
    
    private void OnDisable()
    {
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHitThisSwing)
        {
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
        }
    }
}