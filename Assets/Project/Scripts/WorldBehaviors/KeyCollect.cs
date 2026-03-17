using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class KeyCollect : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.5f;
    
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Text promptText;
    [SerializeField] private string collectMessage = "Press E to Collect Key";
    
    [Header("Key Animation Settings")]
    [SerializeField] private float floatHeight = 0.3f;
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float collectRiseDuration = 0.5f;
    [SerializeField] private float collectRiseHeight = 1.5f;
    [SerializeField] private float collectSpinSpeed = 360f;
    [SerializeField] private float collectSpinAcceleration = 720f;
    [SerializeField] private float collectScaleMultiplier = 1.5f;
    [SerializeField] private float collectShrinkDuration = 0.8f;
    
    [Header("Audio")]
    [SerializeField] private AudioClip collectSound;
    [SerializeField] [Range(0f, 2f)] private float collectSoundVolume = 1.0f;
    [SerializeField] private AudioClip victorySound;
    [SerializeField] [Range(0f, 2f)] private float victorySoundVolume = 1.0f;
    
    [Header("Victory Animation")]
    [SerializeField] private string victoryTriggerName = "isVictory";
    [SerializeField] private float victoryAnimationDuration = 2.0f;
    [SerializeField] private float postVictoryDelay = 0.5f;
    
    [Header("Scene Transition")]
    [SerializeField] private string returnSceneName = "WorldMap";
    [SerializeField] private int nextState = 5;
    [SerializeField] private string returnSpawnPoint = "";
    
    private Transform player;
    private AudioSource playerSFXSource;
    private bool isCollected = false;
    private bool isNearKey = false;
    
    private Vector3 startPosition;
    private float timeOffset;
    private SpriteRenderer keySprite;
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
                playerSFXSource = sfxChild.GetComponent<AudioSource>();
        }
        
        keySprite = GetComponent<SpriteRenderer>();
        
        if (promptUI != null)
            promptUI.SetActive(false);
        
        startPosition = transform.position;
        timeOffset = Random.Range(0f, Mathf.PI * 2f);
    }
    
    private void Update()
    {
        if (isCollected || player == null) return;
        
        IdleFloating();
        
        float distance = Vector2.Distance(transform.position, player.position);
        
        bool wasNear = isNearKey;
        isNearKey = distance <= interactionRadius;
        
        if (isNearKey != wasNear)
        {
            if (isNearKey)
                ShowPrompt();
            else
                HidePrompt();
        }
        
        // New Input System
        if (isNearKey && inputActions.Player.Interact.WasPressedThisFrame())
        {
            StartCoroutine(CollectSequence());
        }
    }
    
    private void IdleFloating()
    {
        float newY = startPosition.y + Mathf.Sin((Time.time * floatSpeed) + timeOffset) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);
    }
    
    private void ShowPrompt()
    {
        if (promptUI == null) return;
        
        if (promptText != null)
            promptText.text = collectMessage;
        
        promptUI.SetActive(true);
    }
    
    private void HidePrompt()
    {
        if (promptUI == null) return;
        promptUI.SetActive(false);
    }
    
    private IEnumerator CollectSequence()
    {
        isCollected = true;
        HidePrompt();
        
        if (playerSFXSource != null && collectSound != null)
            playerSFXSource.PlayOneShot(collectSound, collectSoundVolume);
        
        yield return StartCoroutine(KeyCollectAnimation());
        yield return StartCoroutine(AliVictoryAnimation());
        
        LoadingManager.LoadScene(returnSceneName, nextState, returnSpawnPoint);
    }
    
    private IEnumerator KeyCollectAnimation()
    {
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + Vector3.up * collectRiseHeight;
        Vector3 originalScale = transform.localScale;
        Vector3 maxScale = originalScale * collectScaleMultiplier;
        
        float currentSpinSpeed = collectSpinSpeed;
        float elapsed = 0f;
        
        while (elapsed < collectRiseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectRiseDuration;
            
            transform.position = Vector3.Lerp(startPos, targetPos, t);
            
            float scaleT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(originalScale, maxScale, scaleT);
            
            currentSpinSpeed += collectSpinAcceleration * Time.deltaTime;
            transform.Rotate(Vector3.forward, currentSpinSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        elapsed = 0f;
        
        while (elapsed < collectShrinkDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / collectShrinkDuration;
            
            float shrinkT = 1f - t;
            transform.localScale = maxScale * shrinkT;
            
            currentSpinSpeed += collectSpinAcceleration * Time.deltaTime;
            transform.Rotate(Vector3.forward, currentSpinSpeed * Time.deltaTime);
            
            yield return null;
        }
        
        if (keySprite != null)
            keySprite.enabled = false;
    }
    
    private IEnumerator AliVictoryAnimation()
    {
        if (player == null) yield break;
        
        Animator aliAnimator = player.GetComponent<Animator>();
        if (aliAnimator == null) yield break;
        
        aliAnimator.SetTrigger(victoryTriggerName);
        
        if (playerSFXSource != null && victorySound != null)
            playerSFXSource.PlayOneShot(victorySound, victorySoundVolume);
        
        yield return new WaitForSeconds(victoryAnimationDuration);
        yield return new WaitForSeconds(postVictoryDelay);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}