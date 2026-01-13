using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameOverManager : MonoBehaviour
{
    [Header("Death Sequence")]
    [SerializeField] private float deathRotationDuration = 0.5f;
    [SerializeField] private AnimationCurve deathRotationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AudioClip deathSfx;
    [SerializeField] private float deathSequenceDelay = 0.5f;
    
    [Header("Red Screen Effect")]
    [SerializeField] private Image redScreenOverlay;
    [SerializeField] private float redScreenFadeInDuration = 0.25f;
    [SerializeField] private float redScreenFadeOutDuration = 0.25f;
    [SerializeField] private Color redScreenColor = new Color(1f, 0f, 0f, 0.6f);
    
    [Header("UI Elements")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button mainMenuButton;
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip gameOverSfx;
    [SerializeField] private AudioClip buttonClickSfx;
    
    [Header("State Control")]
    [SerializeField] private int[] allowedStates = { 2, 3, 4, 5, 6, 7, 8, 9, 10 };
    
    private static GameOverManager instance;
    private bool isDead = false;
    
    public static GameOverManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameOverManager>();
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
        
        if (redScreenOverlay != null)
        {
            redScreenOverlay.gameObject.SetActive(true);
            Color col = redScreenOverlay.color;
            col.a = 0f;
            redScreenOverlay.color = col;
        }
        
        if (retryButton != null)
        {
            retryButton.onClick.AddListener(OnRetry);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(OnMainMenu);
        }
        
        Debug.Log("[GameOver] Manager initialized");
    }
    
    public void ShowGameOver()
    {
        if (isDead)
        {
            Debug.LogWarning("[GameOver] Already dead! Ignoring duplicate call.");
            return;
        }
        
        int currentState = PlayerPrefs.GetInt("GameState", 1);
        
        bool isAllowedState = System.Array.Exists(allowedStates, state => state == currentState);
        
        if (!isAllowedState)
        {
            Debug.LogWarning($"[GameOver] State {currentState} is not allowed for Game Over! Skipping...");
            RetryCurrentState();
            return;
        }
        
        isDead = true;
        
        Debug.Log($"[GameOver] Death sequence starting! Current state: {currentState}");
        
        StartCoroutine(DeathSequence());
    }
    
    private System.Collections.IEnumerator DeathSequence()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[GameOver] Player not found! Skipping death sequence.");
            ShowGameOverPanel();
            yield break;
        }
        
        Animator playerAnimator = playerObj.GetComponent<Animator>();
        Transform playerTransform = playerObj.transform;
        Rigidbody2D playerRb = playerObj.GetComponent<Rigidbody2D>();
        PlayerController playerController = playerObj.GetComponent<PlayerController>();
        
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (playerRb != null)
        {
            playerRb.velocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.bodyType = RigidbodyType2D.Static;
        }
        
        Vector3 fixedPosition = playerTransform.position;
        
        if (playerAnimator != null)
        {
            playerAnimator.SetTrigger("isDead");
        }
        
        if (audioSource != null && deathSfx != null)
        {
            audioSource.PlayOneShot(deathSfx);
        }
        
        float elapsed = 0f;
        float startZ = 0f;
        float targetZ = -90f;
        
        while (elapsed < deathRotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / deathRotationDuration;
            float curveT = deathRotationCurve.Evaluate(t);
            
            float newZ = Mathf.Lerp(startZ, targetZ, curveT);
            playerTransform.eulerAngles = new Vector3(0, 0, newZ);
            playerTransform.position = fixedPosition;
            
            yield return null;
        }
        
        playerTransform.eulerAngles = new Vector3(0, 0, targetZ);
        playerTransform.position = fixedPosition;
        
        yield return new WaitForSeconds(deathSequenceDelay);
        
        yield return StartCoroutine(RedScreenFadeIn());
        
        yield return StartCoroutine(RedScreenFadeOut());
        
        ShowGameOverPanel();
    }
    
    private System.Collections.IEnumerator RedScreenFadeIn()
    {
        if (redScreenOverlay == null)
        {
            yield break;
        }
        
        float elapsed = 0f;
        Color startColor = redScreenOverlay.color;
        Color targetColor = redScreenColor;
        
        while (elapsed < redScreenFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redScreenFadeInDuration;
            
            redScreenOverlay.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        redScreenOverlay.color = targetColor;
    }
    
    private System.Collections.IEnumerator RedScreenFadeOut()
    {
        if (redScreenOverlay == null)
        {
            yield break;
        }
        
        float elapsed = 0f;
        Color startColor = redScreenOverlay.color;
        Color targetColor = redScreenColor;
        targetColor.a = 0f;
        
        while (elapsed < redScreenFadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / redScreenFadeOutDuration;
            
            redScreenOverlay.color = Color.Lerp(startColor, targetColor, t);
            
            yield return null;
        }
        
        redScreenOverlay.color = targetColor;
    }
    
    private void ShowGameOverPanel()
    {
        Time.timeScale = 0f;
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        PlayGameOverSfx();
        
        DisablePlayerInput();
    }
    
    public void OnRetry()
    {
        Debug.Log("[GameOver] Retry clicked - Reloading current state...");
        
        PlayButtonClickSfx();
        
        RetryCurrentState();
    }
    
    public void OnMainMenu()
    {
        Debug.Log("[GameOver] Main Menu clicked - Returning to menu...");
        
        PlayButtonClickSfx();
        
        isDead = false;
        
        Time.timeScale = 1f;
        
        SceneManager.LoadScene("MainMenu");
    }
    
    private void RetryCurrentState()
    {
        isDead = false;
        Time.timeScale = 1f;
        
        int currentState = PlayerPrefs.GetInt("GameState", 1);
        
        string targetScene = GetSceneForState(currentState);
        
        Debug.Log($"[GameOver] Retrying state {currentState} in scene: {targetScene}");
        
        LoadingManager.LoadScene(targetScene, currentState, "");
    }
    
    private string GetSceneForState(int state)
    {
        switch (state)
        {
            case 0:
            case 1:
            case 2:
            case 3:
                return "WorldMap";
                
            case 4:
            case 5:
                return "Dungeon1";
                
            case 6:
            case 7:
                return "WorldMap";
                
            case 8:
            case 9:
                return "Dungeon2";
                
            case 10:
                return "WorldMap";
                
            default:
                Debug.LogWarning($"[GameOver] Unknown state {state}, defaulting to WorldMap");
                return "WorldMap";
        }
    }
    
    private void PlayGameOverSfx()
    {
        if (audioSource != null && gameOverSfx != null)
        {
            audioSource.PlayOneShot(gameOverSfx);
        }
    }
    
    private void PlayButtonClickSfx()
    {
        if (audioSource != null && buttonClickSfx != null)
        {
            audioSource.PlayOneShot(buttonClickSfx);
        }
    }
    
    private void DisablePlayerInput()
    {
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player != null)
        {
            player.enabled = false;
        }
    }
    
    private void OnDestroy()
    {
        if (retryButton != null)
        {
            retryButton.onClick.RemoveListener(OnRetry);
        }
        
        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenu);
        }
    }
}