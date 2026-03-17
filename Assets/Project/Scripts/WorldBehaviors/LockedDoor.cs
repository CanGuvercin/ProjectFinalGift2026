using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class LockedDoor : MonoBehaviour
{
    [Header("Lock Icon")]
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private float iconDisplayDuration = 1f;
    
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.2f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] [Range(0f, 5f)] private float soundVolume = 2.0f;
    
    private Transform player;
    private AudioSource playerSFXSource;
    private bool isShowingIcon = false;
    private PlayerInputActions inputActions;
    
    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }
    
    private void OnEnable()
    {
        inputActions.Player.Enable();
    }
    
    private void OnDisable()
    {
        inputActions.Player.Disable();
    }
    
    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            
            Transform sfxChild = playerObj.transform.Find("PlayerSFX");
            if (sfxChild != null)
            {
                playerSFXSource = sfxChild.GetComponent<AudioSource>();
            }
            
            if (playerSFXSource == null)
            {
                Debug.LogWarning("[LockedDoor] PlayerSFX AudioSource not found!");
            }
        }
        
        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (player == null || isShowingIcon) return;
        
        float distance = Vector2.Distance(transform.position, player.position);
        
        // New Input System - Interact action
        if (distance <= interactionRadius && inputActions.Player.Interact.WasPressedThisFrame())
        {
            OnTryOpen();
        }
    }
    
    private void OnTryOpen()
    {
        StartCoroutine(ShowLockIcon());
        
        if (playerSFXSource != null && lockedSound != null)
        {
            playerSFXSource.PlayOneShot(lockedSound, soundVolume);
        }
        
        Debug.Log($"[LockedDoor] Door locked! Playing sound at volume: {soundVolume}");
    }
    
    private IEnumerator ShowLockIcon()
    {
        isShowingIcon = true;
        
        if (lockIcon != null)
        {
            lockIcon.SetActive(true);
            yield return new WaitForSeconds(iconDisplayDuration);
            lockIcon.SetActive(false);
        }
        
        isShowingIcon = false;
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}