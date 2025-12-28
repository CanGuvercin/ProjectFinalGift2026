using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHitBox : MonoBehaviour
{
    private CameraController cameraController;
    
    private void Start()
    {
        cameraController = Camera.main.GetComponent<CameraController>();
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Enemy'ye hit!
        if (other.CompareTag("Enemy") || other.GetComponent<SlimeEnemy>() != null)
        {
            if (cameraController != null)
                cameraController.OnAttackHit();
            
            Debug.Log("[HITBOX] Hit enemy!");
        }
    }
}