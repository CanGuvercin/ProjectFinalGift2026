using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class TutorialManager : MonoBehaviour
{
    [Header("Tutorial Objectives")]
    [SerializeField] private GameObject[] slimes;
    [SerializeField] private GameObject medicPack;
    [SerializeField] private GameObject door; // Locked door reference
    
    [Header("Exit Barrier")]
    [SerializeField] private GameObject exitBarrier;
    
    [Header("State Transition")]
    [SerializeField] private int nextState = 2;
    [SerializeField] private Transform player;
    [SerializeField] private Vector2 transitionPosition;
    [SerializeField] private float transitionRadius = 1f;
    
    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialCanvas; // DialogBox background
    [SerializeField] private TextMeshProUGUI tutorialText; // TMP text
    [SerializeField] private float proximityRadius = 3f; // Yakınlık mesafesi
    
    [Header("Tutorial Messages")]
    [TextArea(2, 4)]
    [SerializeField] private string attackMessageKeyboard = "[J] Press J to Attack!";
    [TextArea(2, 4)]
    [SerializeField] private string attackMessageGamepad = "[A] Press A to Attack!";
    
    [TextArea(2, 4)]
    [SerializeField] private string medicMessageKeyboard = "Walk near the HP Pack to collect it!";
    [TextArea(2, 4)]
    [SerializeField] private string medicMessageGamepad = "Walk near the HP Pack to collect it!";
    
    [TextArea(2, 4)]
    [SerializeField] private string doorMessageKeyboard = "[E] Press E to interact with the door";
    [TextArea(2, 4)]
    [SerializeField] private string doorMessageGamepad = "[A] Press A to interact with the door";
    
    [TextArea(2, 4)]
    [SerializeField] private string completionMessage = "Tutorial complete! Head to the exit!";
    
    [Header("Freeze Settings")]
    [SerializeField] private float freezeDuration = 3f;
    [SerializeField] private bool canSkipWithSpace = true;
    
    private bool tutorialComplete = false;
    private bool stateTransitioned = false;
    
    // Tutorial step tracking
    private bool attackPromptShown = false;
    private bool medicPromptShown = false;
    private bool doorPromptShown = false;
    
    private bool isFrozen = false;
    private float freezeTimer = 0f;
    
    private PlayerController playerController; // YENİ!
    
    public static TutorialManager instance;
    
    private void Awake()
    {
        instance = this;
        
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = playerObj.GetComponent<PlayerController>(); // YENİ!
            }
        }
    }
    
    private void Start()
    {
        // Exit barrier aktif
        if (exitBarrier != null)
        {
            exitBarrier.SetActive(true);
        }
        
        // Tutorial UI başta kapalı
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
    }
    
    private void Update()
    {
        // Freeze kontrolü
        if (isFrozen)
        {
            HandleFreezeState();
            return; // Freeze sırasında başka işlem yapma
        }
        
        if (!tutorialComplete)
        {
            CheckProximityPrompts();
            CheckObjectives();
        }
        else if (!stateTransitioned)
        {
            CheckStateTransition();
        }
    }
    
    private void CheckProximityPrompts()
    {
        if (player == null) return;
        
        // 1. ATTACK PROMPT - Enemy yakınında
        if (!attackPromptShown)
        {
            foreach (GameObject slime in slimes)
            {
                if (slime != null && slime.activeInHierarchy)
                {
                    float distance = Vector2.Distance(player.position, slime.transform.position);
                    if (distance <= proximityRadius)
                    {
                        ShowAttackPrompt();
                        break;
                    }
                }
            }
        }
        
        // 2. MEDIC PROMPT - MedicPack yakınında
        if (!medicPromptShown && medicPack != null && medicPack.activeInHierarchy)
        {
            float distance = Vector2.Distance(player.position, medicPack.transform.position);
            if (distance <= proximityRadius)
            {
                ShowMedicPrompt();
            }
        }
        
        // 3. DOOR PROMPT - Door yakınında
        if (!doorPromptShown && door != null)
        {
            float distance = Vector2.Distance(player.position, door.transform.position);
            if (distance <= proximityRadius)
            {
                ShowDoorPrompt();
            }
        }
    }
    
    private void ShowAttackPrompt()
    {
        attackPromptShown = true;
        
        string message = IsGamepadActive() ? attackMessageGamepad : attackMessageKeyboard;
        ShowPromptAndFreeze(message);
    }
    
    private void ShowMedicPrompt()
    {
        medicPromptShown = true;
        
        string message = IsGamepadActive() ? medicMessageGamepad : medicMessageKeyboard;
        ShowPromptAndFreeze(message);
    }
    
    private void ShowDoorPrompt()
    {
        doorPromptShown = true;
        
        string message = IsGamepadActive() ? doorMessageGamepad : doorMessageKeyboard;
        ShowPromptAndFreeze(message);
    }
    
    private void ShowPromptAndFreeze(string message)
    {
        // UI göster
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true);
        }
        
        if (tutorialText != null)
        {
            tutorialText.text = message;
        }
        
        // Player kontrolünü kapat (YENİ!)
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // Oyunu dondur
        Time.timeScale = 0f;
        isFrozen = true;
        freezeTimer = 0f;
        
        Debug.Log($"[Tutorial] Showing prompt: {message}");
    }
    
    private void HandleFreezeState()
    {
        // Zamansız delta time kullan (freeze sırasında çalışır)
        freezeTimer += Time.unscaledDeltaTime;
        
        // Space tuşu ile skip
        if (canSkipWithSpace && Input.GetKeyDown(KeyCode.Space))
        {
            UnfreezeGame();
            return;
        }
        
        // Süre doldu mu?
        if (freezeTimer >= freezeDuration)
        {
            UnfreezeGame();
        }
    }
    
    private void UnfreezeGame()
    {
        // Player kontrolünü aç (YENİ!)
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // Oyunu devam ettir
        Time.timeScale = 1f;
        isFrozen = false;
        
        // UI gizle
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
        
        Debug.Log("[Tutorial] Game unfrozen!");
    }
    
    private bool IsGamepadActive()
    {
        return Gamepad.current != null && Gamepad.current.wasUpdatedThisFrame;
    }
    
    private void CheckObjectives()
    {
        // Slime'lar yok mu kontrol et
        int aliveSlimes = 0;
        foreach (GameObject slime in slimes)
        {
            if (slime != null && slime.activeInHierarchy)
            {
                aliveSlimes++;
            }
        }
        
        // MedicPack yok mu kontrol et
        bool medicPackCollected = (medicPack == null || !medicPack.activeInHierarchy);
        
        // Tüm hedefler tamamlandı mı?
        if (aliveSlimes == 0 && medicPackCollected)
        {
            CompleteTutorial();
        }
    }
    
    private void CompleteTutorial()
    {
        tutorialComplete = true;
        
        Debug.Log("[Tutorial] TUTORIAL COMPLETE!");
        
        // Exit barrier'ı kaldır
        if (exitBarrier != null)
        {
            exitBarrier.SetActive(false);
        }
        
        // Tamamlanma mesajı (freeze YOK!)
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(true);
        }
        if (tutorialText != null)
        {
            tutorialText.text = completionMessage;
        }
        
        // 3 saniye sonra gizle
        Invoke(nameof(HideCompletionMessage), 3f);
    }
    
    private void HideCompletionMessage()
    {
        if (tutorialCanvas != null)
        {
            tutorialCanvas.SetActive(false);
        }
    }
    
    private void CheckStateTransition()
    {
        if (player == null) return;
        
        float distance = Vector2.Distance(player.position, transitionPosition);
        
        if (distance <= transitionRadius)
        {
            TransitionToNextState();
        }
    }
    
    private void TransitionToNextState()
    {
        stateTransitioned = true;
        
        Debug.Log($"[Tutorial] Transitioning to State {nextState}");
        
        CutsceneChief cutsceneChief = FindObjectOfType<CutsceneChief>();
        if (cutsceneChief != null)
        {
            cutsceneChief.SetState(nextState);
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        // State transition point
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transitionPosition, transitionRadius);
        
        // Proximity radius (Enemy)
        if (slimes != null && slimes.Length > 0)
        {
            Gizmos.color = Color.red;
            //..
            foreach (GameObject slime in slimes)
            {
                if (slime != null)
                {
                    Gizmos.DrawWireSphere(slime.transform.position, proximityRadius);
                }
            }
        }
        
        // Proximity radius (MedicPack)
        if (medicPack != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(medicPack.transform.position, proximityRadius);
        }
        
        // Proximity radius (Door)
        if (door != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(door.transform.position, proximityRadius);
        }
    }
}