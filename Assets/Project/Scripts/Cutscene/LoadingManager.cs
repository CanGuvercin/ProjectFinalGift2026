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
    [Tooltip("Loading ekranı en az bu kadar gösterilir (çok hızlı geçmesin diye)")]
    
    [Header("Loading Text Animation")]
    [SerializeField] private string[] loadingFrames = { 
        "Loading", 
        "Loading .", 
        "Loading . .", 
        "Loading . . ." 
    };
    [SerializeField] private float textFrameDuration = 0.25f;
    
    // Static variables (scene geçişlerinde korunur)
    private static string targetSceneName;
    private static int targetState = -1;
    private static string spawnPointName;
    
    private Coroutine textAnimationCoroutine;
    
    private void Start()
    {
        Debug.Log($"[LoadingManager] ═══════════════════════════════");
        Debug.Log($"[LoadingManager] Started!");
        Debug.Log($"[LoadingManager] Target Scene: {targetSceneName}");
        Debug.Log($"[LoadingManager] Target State: {targetState}");
        Debug.Log($"[LoadingManager] Spawn Point: {spawnPointName}");
        Debug.Log($"[LoadingManager] ═══════════════════════════════");

        Animator aliAnimator = GameObject.Find("AliRunning")?.GetComponent<Animator>();
        if (aliAnimator != null)
        {
            aliAnimator.updateMode = AnimatorUpdateMode.UnscaledTime;
            Debug.Log("[LoadingManager] Ali animator set to Unscaled Time");
        }
        
        // Fade overlay setup
        if (fadeOverlay != null)
        {
            // Canvas'ı aktif et (yoksa fade görünmez!)
            fadeOverlay.gameObject.SetActive(true);
            
            // Başta tamamen OPAK (siyah ekran)
            fadeOverlay.alpha = 1f;
            fadeOverlay.blocksRaycasts = true; // Input'u engelle
            
            Debug.Log("[LoadingManager] ✅ FadeOverlay initialized (starting BLACK)");
        }
        else
        {
            Debug.LogError("[LoadingManager] ❌ FadeOverlay is NULL! Fade effects will not work.");
        }
        
        // Loading text animation başlat
        if (loadingText != null)
        {
            textAnimationCoroutine = StartCoroutine(AnimateLoadingText());
        }
        
        // Ana loading sequence
        StartCoroutine(LoadSceneSequence());
    }
    
    /// <summary>
    /// STATIC METHOD: Target state'i döndürür (CutsceneChief için)
    /// </summary>
    public static int GetTargetState()
    {
        return targetState;
    }
    
    /// <summary>
    /// STATIC METHOD: Spawn point adını döndürür
    /// </summary>
    public static string GetSpawnPoint()
    {
        return spawnPointName;
    }
    
    /// <summary>
    /// STATIC METHOD: State ve spawn point'i temizle (CutsceneChief okuduktan sonra çağrılır)
    /// </summary>
    public static void ClearTransitionData()
    {
        targetState = -1;
        spawnPointName = "";
        Debug.Log("[LoadingManager] Transition data cleared");
    }
    
    /// <summary>
    /// STATIC METHOD: Dışarıdan scene yüklemek için
    /// Usage: LoadingManager.LoadScene("WorldMap", 5, "Dungeon1GateSpawn");
    /// </summary>
    public static void LoadScene(string sceneName, int newState = -1, string spawnPoint = "")
    {
        Debug.Log($"[LoadingManager] ═══ LOAD REQUEST ═══");
        Debug.Log($"[LoadingManager] Scene: {sceneName}");
        Debug.Log($"[LoadingManager] State: {newState}");
        Debug.Log($"[LoadingManager] Spawn: {spawnPoint}");
        
        // Parametreleri kaydet
        targetSceneName = sceneName;
        targetState = newState;
        spawnPointName = spawnPoint;
        
        // State'i kaydet (loading screen'den ÖNCE!)
        if (newState >= 0)
        {
            PlayerPrefs.SetInt("GameState", newState);
            PlayerPrefs.Save();
            Debug.Log($"[LoadingManager] State saved: {newState}");
        }
        
        // Spawn point'i kaydet
        if (!string.IsNullOrEmpty(spawnPoint))
        {
            PlayerPrefs.SetString("SpawnPoint", spawnPoint);
            PlayerPrefs.Save();
            Debug.Log($"[LoadingManager] Spawn point saved: {spawnPoint}");
        }
        
        // Loading scene'ini yükle
        SceneManager.LoadScene("LoadingScene");
    }
    
    private IEnumerator LoadSceneSequence()
    {
        float startTime = Time.realtimeSinceStartup;
        
        // Scene name kontrolü
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogError("[LoadingManager] ❌ Target scene name is empty! Cannot load scene.");
            Debug.LogError("[LoadingManager] This usually means LoadingScene was opened directly without calling LoadScene() method.");
            yield break;
        }
        
        // PHASE 1: FADE IN (Loading ekranı beliriyor)
        Debug.Log("[LoadingManager] Phase 1: Fade In");
        yield return StartCoroutine(FadeIn());
        
        // PHASE 2: ASYNC SCENE LOADING
        Debug.Log($"[LoadingManager] Phase 2: Loading {targetSceneName}...");
        
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        asyncLoad.allowSceneActivation = false; // Manuel aktivasyon
        
        // Scene yüklenirken bekle
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            
            // Progress log (debug)
            if (progress >= 0.9f)
            {
                Debug.Log($"[LoadingManager] Scene ready! Progress: {progress * 100f:F0}%");
            }
            
            // Scene hazır VE minimum süre geçti mi?
            float elapsedTime = Time.realtimeSinceStartup - startTime;
            
            if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadTime)
            {
                Debug.Log($"[LoadingManager] Minimum time passed ({elapsedTime:F2}s)");
                break;
            }
            
            yield return null;
        }
        
        // PHASE 3: FADE OUT (Loading ekranı kayboluyor)
        Debug.Log("[LoadingManager] Phase 3: Fade Out");
        yield return StartCoroutine(FadeOut());
        
        // PHASE 4: SCENE ACTIVATION
        Debug.Log("[LoadingManager] Phase 4: Activating scene...");
        asyncLoad.allowSceneActivation = true;
        
        // NOT: targetState ve spawnPointName artık CutsceneChief tarafından temizleniyor
        
        float totalTime = Time.realtimeSinceStartup - startTime;
        Debug.Log($"[LoadingManager] ✅ COMPLETE! Total time: {totalTime:F2}s");
        Debug.Log($"[LoadingManager] ═══════════════════════════════");
    }
    
    private IEnumerator FadeIn()
    {
        if (fadeOverlay == null)
        {
            Debug.LogWarning("[LoadingManager] FadeOverlay is null! Skipping fade in.");
            yield break;
        }
        
        float elapsed = 0f;
        
        // Karanlıktan aydınlığa (1 → 0)
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeInDuration);
            yield return null;
        }
        
        fadeOverlay.alpha = 0f;
        Debug.Log("[LoadingManager] Fade In complete (now BRIGHT)");
    }
    
    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null)
        {
            Debug.LogWarning("[LoadingManager] FadeOverlay is null! Skipping fade out.");
            yield break;
        }
        
        float elapsed = 0f;
        
        // Aydınlıktan karanlığa (0 → 1)
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            fadeOverlay.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeOutDuration);
            yield return null;
        }
        
        fadeOverlay.alpha = 1f;
        Debug.Log("[LoadingManager] Fade Out complete (now BLACK)");
    }
    
    private IEnumerator AnimateLoadingText()
    {
        if (loadingText == null)
        {
            Debug.LogWarning("[LoadingManager] LoadingText is null! Skipping text animation.");
            yield break;
        }
        
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
        // Coroutine'leri temizle
        if (textAnimationCoroutine != null)
        {
            StopCoroutine(textAnimationCoroutine);
        }
    }
}