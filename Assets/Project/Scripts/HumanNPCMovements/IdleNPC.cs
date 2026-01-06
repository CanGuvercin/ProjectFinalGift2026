using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleNPC : MonoBehaviour
{
    private Animator animator;
    
    void Start()
    {
        animator.SetBool("isIdle", true);
    }
}
