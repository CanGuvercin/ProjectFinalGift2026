using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TalkNPC : MonoBehaviour
{
    [SerializeField] private float talkInterval = 3f;
    
    private Animator animator;
    private float timer;
    
    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= talkInterval)
        {
            animator.SetTrigger("Talk");
            timer = 0f;
        }
    }
}
