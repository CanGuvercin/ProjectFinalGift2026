using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;

public class IntroSceneLoader : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string targetSceneName = "MainMenu";
    
    [Header("References")]
    [SerializeField] private PlayableDirector playableDirector;
    
    [Header("Timing")]
    [SerializeField] private float additionalDelay = 0f; // İsterseniz ekstra delay
    
    [Header("Debug")]
    [SerializeField] private bool showDebugLogs = true;

    private void Awake()
    {
        Shader.WarmupAllShaders();
        // PlayableDirector otomatik bul (aynı GameObject'te yoksa)
        if (playableDirector == null)
        {
            playableDirector = FindObjectOfType<PlayableDirector>();
        }

        if (playableDirector == null)
        {
            Debug.LogError("[IntroSceneLoader] PlayableDirector bulunamadı!");
            return;
        }

        if (showDebugLogs)
            Debug.Log("[IntroSceneLoader] Script hazır. Timeline bitince MainMenu yüklenecek.");
    }

    

    private void OnEnable()
    {
        if (playableDirector != null)
        {
            // Timeline bittiğinde bu method çağrılacak
            playableDirector.stopped += OnTimelineFinished;
            
            if (showDebugLogs)
                Debug.Log("[IntroSceneLoader] Timeline dinleniyor...");
        }
    }

    private void OnDisable()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnTimelineFinished;
        }
    }

    private void OnTimelineFinished(PlayableDirector director)
    {
        if (showDebugLogs)
            Debug.Log($"[IntroSceneLoader] ✅ Timeline bitti! {additionalDelay} saniye sonra sahne yüklenecek.");

        if (additionalDelay > 0)
        {
            Invoke(nameof(LoadTargetScene), additionalDelay);
        }
        else
        {
            LoadTargetScene();
        }
    }

    private void LoadTargetScene()
    {
        if (showDebugLogs)
            Debug.Log($"[IntroSceneLoader] Sahne yükleniyor: {targetSceneName}");

        if (Application.CanStreamedLevelBeLoaded(targetSceneName))
        {
            SceneManager.LoadScene(targetSceneName);
        }
        else
        {
            Debug.LogError($"[IntroSceneLoader] ❌ HATA: '{targetSceneName}' sahnesi Build Settings'de bulunamadı!");
        }
    }

    // Manuel test için (Inspector'da sağ tık > Test Load Scene)
    [ContextMenu("Test Load Scene")]
    private void TestLoadScene()
    {
        LoadTargetScene();
    }
}