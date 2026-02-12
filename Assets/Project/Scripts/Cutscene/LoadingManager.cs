using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Text loadingText;
    [SerializeField] private CanvasGroup fadeOverlay;
    
    [Header("Fade Settings")]
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.8f;
    
    [Header("Loading Settings")]
    [SerializeField] private float minimumLoadTime = 1.5f;
    
    [Header("Loading Text Animation")]
    [SerializeField] private string[] loadingFrames = { 
        "Loading", 
        "Loading .", 
        "Loading . .", 
        "Loading . . ." 
    };
    [SerializeField] private float textFrameDuration = 0.25f;
    
    private static string targetSceneName;
    private static int targetState = -1;
    private static string spawnPointName;
    
    private Coroutine textAnimationCoroutine;
    
    private void Start()
    {
        Debug.Log($"[LoadingManager] Started! Target Scene: {targetSceneName} | State: {targetState} | Spawn: {spawnPointName}");

        Animator aliAnimator = GameObject.Find("AliRunning")?.GetComponent<Animator>();
        if (aliAnimator != null)
            aliAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
        
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true;
        }
        else
        {
            Debug.LogError("[LoadingManager] ❌ FadeOverlay is NULL!");
        }
        
        if (loadingText != null)
            textAnimationCoroutine = StartCoroutine(AnimateLoadingText());
        
        StartCoroutine(LoadSceneSequence());
    }
    
    public static int GetTargetState()
    {
        return targetState;
    }
    
    public static string GetSpawnPoint()
    {
        return spawnPointName;
    }
    
    public static void ClearTransitionData()
    {
        targetState = -1;
        spawnPointName = "";
        Debug.Log("[LoadingManager] Transition data cleared");
    }
    
    public static void LoadScene(string sceneName, int newState = -1, string spawnPoint = "")
    {
        Debug.Log($"[LoadingManager] LOAD REQUEST → Scene: {sceneName} | State: {newState} | Spawn: {spawnPoint}");
        
        targetSceneName = sceneName;
        targetState = newState;
        spawnPointName = spawnPoint;
        
        if (newState >= 0)
        {
            PlayerPrefs.SetInt("GameState", newState);
            PlayerPrefs.SetString("GameScene", sceneName); // ← YENİ: Hangi sahnede olduğunu kaydet
            PlayerPrefs.Save();
            Debug.Log($"[LoadingManager] State saved: {newState} | Scene saved: {sceneName}");
        }
        
        if (!string.IsNullOrEmpty(spawnPoint))
        {
            PlayerPrefs.SetString("SpawnPoint", spawnPoint);
            PlayerPrefs.Save();
        }
        
        SceneManager.LoadScene("LoadingScene");
    }
    
    private IEnumerator LoadSceneSequence()
    {
        float startTime = Time.realtimeSinceStartup;
        
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[LoadingManager] ❌ Target scene name is empty!");
            yield break;
        }
        
        yield return StartCoroutine(FadeIn());
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false;
        
        while (!asyncLoad.isDone)
        {
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            
            if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadTime)
                break;
            
            yield return null;
        }
        
        yield return StartCoroutine(FadeOut());
        
        asyncLoad.allowSceneActivation = true;
        
        Debug.Log($"[LoadingManager] ✅ COMPLETE! Total time: {Time.realtimeSinceStartup - startTime:F2}s");
    }
    
    private IEnumerator FadeIn()
    {
        if (fadeOverlay == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }
        fadeOverlay.alpha = 0f;
    }
    
    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null) yield break;
        
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }
        fadeOverlay.alpha = 1f;
    }
    
    private IEnumerator AnimateLoadingText()
    {
        if (loadingText == null) yield break;
        
        int index = 0;
        while (true)
        {
            loadingText.text = loadingFrames[index];
            index = (index + 1) % loadingFrames.Length;
            yield return new WaitForSeconds(textFrameDuration);
        }
    }
    
    private void OnDestroy()
    {
        if (textAnimationCoroutine != null)
            StopCoroutine(textAnimationCoroutine);
    }
}