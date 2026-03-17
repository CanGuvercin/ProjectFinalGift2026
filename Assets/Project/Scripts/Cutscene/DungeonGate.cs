using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

public class DungeonGate : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private float interactionRadius = 1.2f;
    
    [Header("UI Prompt")]
    [SerializeField] private GameObject promptUI;
    [SerializeField] private Text promptText;
    [SerializeField] private string enterMessage = "Press E to Enter Dungeon";
    
    [Header("Audio")]
    [SerializeField] private AudioClip enterSfx;
    [SerializeField] [Range(0f, 2f)] private float soundVolume = 1.0f;
    
    private Transform player;
    private AudioSource playerSFXSource;
    private CutsceneChief cutsceneChief;
    private bool isNearGate = false;
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
        }
        
        cutsceneChief = FindObjectOfType<CutsceneChief>();
        
        if (promptUI != null)
            promptUI.SetActive(false);
    }
    
    private void Update()
    {
        if (player == null || cutsceneChief == null) return;
        
        float distance = Vector2.Distance(transform.position, player.position);
        
        bool wasNear = isNearGate;
        isNearGate = distance <= interactionRadius;
        
        if (isNearGate != wasNear)
        {
            if (isNearGate)
                ShowPrompt();
            else
                HidePrompt();
        }
        
        // New Input System - Interact action
        if (isNearGate && inputActions.Player.Interact.WasPressedThisFrame())
        {
            EnterDungeon();
        }
    }
    
    private void ShowPrompt()
    {
        if (promptUI == null) return;
        
        if (promptText != null)
            promptText.text = enterMessage;
        
        promptUI.SetActive(true);
    }
    
    private void HidePrompt()
    {
        if (promptUI == null) return;
        promptUI.SetActive(false);
    }
    
    private void EnterDungeon()
    {
        if (playerSFXSource != null && enterSfx != null)
            playerSFXSource.PlayOneShot(enterSfx, soundVolume);
        
        HidePrompt();
        cutsceneChief.AdvanceState();
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
    }
}//