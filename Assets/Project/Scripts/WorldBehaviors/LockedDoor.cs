using UnityEngine;
using System.Collections;

public class LockedDoor : MonoBehaviour
{
    [Header("Lock Icon")]
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private float iconDisplayDuration = 1f;
    
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    
    [Header("Audio")]
    [SerializeField] private AudioClip lockedSound;
    [SerializeField] [Range(0f, 5f)] private float soundVolume = 2.0f; // 0-5 arası slider
    
    private Transform player;
    private AudioSource playerSFXSource;
    private bool isShowingIcon = false;
    
    private void Start()
    {
        // Player'ı bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            player = playerObj.transform;
            
            // PlayerSFX AudioSource'u bul
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
        
        // Lock icon başta kapalı
        if (lockIcon != null)
        {
            lockIcon.SetActive(false);
        }
    }
    
    private void Update()
    {
        if (player == null || isShowingIcon) return;
        
        // Player yakınında mı?
        float distance = Vector2.Distance(transform.position, player.position);
        
        // E tuşuna basıldı mı?
        if (distance <= interactionRadius && Input.GetKeyDown(interactKey))
        {
            OnTryOpen();
        }
    }
    
    private void OnTryOpen()
    {
        // Lock icon göster
        StartCoroutine(ShowLockIcon());
        
        // SFX çal (Player'ın AudioSource'undan - YÜKSEK VOLUME)
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
    
    // Debug: Interaction radius göster
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}