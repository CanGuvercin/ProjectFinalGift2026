using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolNPC : MonoBehaviour
{
    [SerializeField] private Transform pointA;
    [SerializeField] private Transform pointB;
    [SerializeField] private float speed = 1f;
    
    private Animator animator;
    private bool movingToB = true;
    
    void Update()
    {
        Transform target = movingToB ? pointB : pointA;
        transform.position = Vector3.MoveTowards(transform.position, target.position, speed * Time.deltaTime);
        
        if (Vector3.Distance(transform.position, target.position) < 0.1f)
        {
            movingToB = !movingToB;
            // Flip sprite
        }
        
        animator.SetBool("isWalking", true);
    }
}
