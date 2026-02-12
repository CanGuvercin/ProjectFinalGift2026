using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;

public class CutsceneChief : MonoBehaviour
{
    [System.Serializable]
    public class CutsceneState
    {
        public string stateName;
        public PlayableDirector timeline;
        public GameObject[] objectsToActivate;
        public GameObject[] objectsToDeactivate;
        public Transform playerSpawnPosition;
        
        [Header("Scene Change")]
        public bool changeScene = false;
        public string targetSceneName = "";
        public string spawnPointName = "";
        
        [Header("Music")]
        public AudioClip ambientMusic;
        [Range(0f, 1f)] public float musicVolume = 0.5f;
        public bool fadeMusic = true;
    }
    
    [System.Serializable]
    public class SceneSafetyRule
    {
        public string sceneName;
        public int requiredState;
        public Vector3 emergencySpawnPosition;
        public bool strictMode = true;
    }
    
    [Header("Scene Safety Rules")]
    [SerializeField] private SceneSafetyRule[] sceneSafetyRules;
    
    [Header("Cutscene States")]
    [SerializeField] private CutsceneState[] cutsceneStates;
    
    [Header("Current State")]
    [SerializeField] private int currentState = 0;
    
    [Header("Save Key")]
    [SerializeField] private string saveKey = "GameState";
    
    [Header("Music Settings")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float fadeDuration = 1.5f;
    
    [Header("Save UI")]
    [SerializeField] private GameObject gameSavedCanvas;
    
    [Header("End Credits Settings")]
    [SerializeField] private float endCreditsDelay = 23f;
    
    [Header("Debug UI")]
    [SerializeField] private TextMeshProUGUI debugStateNumberText;
    [SerializeField] private TextMeshProUGUI debugStateNameText;
    
    private Coroutine musicFadeCoroutine;
    private Coroutine saveUICoroutine;
    private Coroutine endCreditsCoroutine;
    
    private bool shouldAutoAdvanceOnTimelineStop = true;
    
    private void Awake()
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("CutsceneMusic");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (gameSavedCanvas != null)
            gameSavedCanvas.SetActive(false);
    }
    
    private void UpdateDebugUI()
    {
        if (debugStateNumberText != null)
            debugStateNumberText.text = $"State: {currentState}";
        
        if (debugStateNameText != null)
        {
            if (currentState >= 0 && currentState < cutsceneStates.Length)
                debugStateNameText.text = cutsceneStates[currentState].stateName;
            else
                debugStateNameText.text = "INVALID";
        }
    }
    
    private void Start()
    {
        int loadingState = LoadingManager.GetTargetState();
        
        if (loadingState >= 0)
        {
            if (loadingState >= cutsceneStates.Length)
            {
                Debug.LogError($"[CutsceneChief] ❌ INVALID LoadingManager state: {loadingState} (max: {cutsceneStates.Length - 1})");
                loadingState = Mathf.Clamp(loadingState, 0, cutsceneStates.Length - 1);
            }
            
            Debug.Log($"[CutsceneChief] State override from LoadingManager: {loadingState}");
            currentState = loadingState;
            SaveState();
            LoadingManager.ClearTransitionData();
        }
        else
        {
            LoadState();
        }
        
        ValidateSceneSafety();
        UpdateDebugUI();
        
        if (currentState == 21)
        {
            Debug.Log("[CutsceneChief] 🎬 FINAL STATE - End Credits");
            ShowGameSavedUI();
            PlayCurrentState();
            StartEndCreditsSequence();
            return;
        }
        else
        {
            PlayCurrentState();
        }
    }
    
    private void ValidateSceneSafety()
    {
        if (sceneSafetyRules == null || sceneSafetyRules.Length == 0)
        {
            Debug.Log("[CutsceneChief] 🔓 No safety rules defined");
            return;
        }
        
        string currentSceneName = SceneManager.GetActiveScene().name;
        
        foreach (SceneSafetyRule rule in sceneSafetyRules)
        {
            if (rule.sceneName == currentSceneName)
            {
                Debug.Log($"[CutsceneChief] 🔐 Safety rule found for '{currentSceneName}' - Required State: {rule.requiredState}");
                
                int savedState = PlayerPrefs.GetInt(saveKey, currentState);
                
                if (savedState != rule.requiredState)
                {
                    Debug.LogWarning($"[CutsceneChief] ⚠️ STATE MISMATCH! Expected: {rule.requiredState} Found: {savedState}");
                    
                    if (rule.strictMode)
                    {
                        Debug.Log($"[CutsceneChief] 🔧 STRICT MODE - Forcing state to {rule.requiredState}");
                        currentState = rule.requiredState;
                        SaveState();
                    }
                }
                else
                {
                    Debug.Log($"[CutsceneChief] ✅ State validated: {savedState}");
                }
                
                return;
            }
        }
        
        Debug.Log($"[CutsceneChief] 🔓 No safety rule for '{currentSceneName}'");
    }
    
    private void StartEndCreditsSequence()
    {
        Debug.Log($"[CutsceneChief] ⏱️ End credits will finish in {endCreditsDelay} seconds...");
        
        if (endCreditsCoroutine != null)
            StopCoroutine(endCreditsCoroutine);
        
        endCreditsCoroutine = StartCoroutine(EndCreditsTimer());
    }
    
    private IEnumerator EndCreditsTimer()
{
    yield return new WaitForSeconds(endCreditsDelay);
    
    Debug.Log("[CutsceneChief] ========== 🎬 GAME COMPLETED 🎬 ==========");
    
    PlayerPrefs.DeleteKey(saveKey);
    PlayerPrefs.Save();
    
    // ESKİ: LoadingManager.LoadScene("MainMenu"); ← LoadingScene üzerinden gidiyor, siyah kalıyor
    // YENİ: Direkt geç
    SceneManager.LoadScene("MainMenu");
}
    
    public void PlayCurrentState()
    {
        if (currentState < 0 || currentState >= cutsceneStates.Length)
        {
            Debug.LogError($"[CutsceneChief] ❌ Invalid state: {currentState} (max: {cutsceneStates.Length - 1})");
            return;
        }
        
        CutsceneState state = cutsceneStates[currentState];
        Debug.Log($"[CutsceneChief] === Playing State {currentState}: {state.stateName} ===");
        
        UpdateDebugUI();
        HandleMusic(state);
        SyncCameraPositions(state);
        
        if (state.objectsToDeactivate != null)
            foreach (GameObject obj in state.objectsToDeactivate)
                if (obj != null) obj.SetActive(false);
        
        if (state.objectsToActivate != null)
            foreach (GameObject obj in state.objectsToActivate)
                if (obj != null) obj.SetActive(true);
        
        SpawnPlayer(state);
        
        if (state.timeline != null)
        {
            state.timeline.stopped -= OnTimelineStopped;
            state.timeline.Play();
            state.timeline.stopped += OnTimelineStopped;
            Debug.Log($"[State {currentState}] Timeline started: {state.timeline.name}");
        }
        else
        {
            Debug.Log($"[State {currentState}] No timeline, state ready");
        }
    }
    
    private void SpawnPlayer(CutsceneState state)
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        
        if (player == null)
        {
            Debug.LogError("[CutsceneChief] ❌ CRITICAL: Player not found in scene!");
            return;
        }
        
        string spawnPointName = LoadingManager.GetSpawnPoint();
        Vector3? spawnPosition = null;
        string spawnSource = "UNKNOWN";
        
        if (!string.IsNullOrEmpty(spawnPointName))
        {
            GameObject spawnPoint = GameObject.Find(spawnPointName);
            if (spawnPoint != null)
            {
                spawnPosition = spawnPoint.transform.position;
                spawnSource = $"LoadingManager ({spawnPointName})";
            }
            else
            {
                Debug.LogWarning($"[State {currentState}] ⚠️ Spawn point '{spawnPointName}' not found!");
            }
        }
        
        if (!spawnPosition.HasValue && state.playerSpawnPosition != null)
        {
            spawnPosition = state.playerSpawnPosition.position;
            spawnSource = "State Spawn Point";
        }
        
        if (!spawnPosition.HasValue && sceneSafetyRules != null)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            foreach (SceneSafetyRule rule in sceneSafetyRules)
            {
                if (rule.sceneName == currentSceneName)
                {
                    spawnPosition = rule.emergencySpawnPosition;
                    spawnSource = "EMERGENCY SPAWN (Safety Rule)";
                    Debug.LogWarning($"[State {currentState}] ⚠️ Using emergency spawn!");
                    break;
                }
            }
        }
        
        if (spawnPosition.HasValue)
        {
            // ═══ BUILD FIX: Rigidbody + Physics sync ═══
            Rigidbody2D rb = player.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.position = spawnPosition.Value;
            }
            
            player.transform.position = spawnPosition.Value;
            Physics2D.SyncTransforms();
            
            Debug.Log($"[State {currentState}] ✅ Player spawned at: {spawnSource} → {spawnPosition.Value}");
        }
        else
        {
            Debug.LogError($"[State {currentState}] ❌ CRITICAL: No spawn position available!");
        }
    }
    
    private void HandleMusic(CutsceneState newState)
    {
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("CutsceneMusic");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (newState.ambientMusic == null) return;
        if (musicSource.clip == newState.ambientMusic && musicSource.isPlaying) return;
        
        if (newState.fadeMusic)
        {
            if (musicFadeCoroutine != null)
                StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = StartCoroutine(FadeToNewMusic(newState.ambientMusic, newState.musicVolume));
        }
        else
        {
            musicSource.Stop();
            musicSource.clip = newState.ambientMusic;
            musicSource.volume = newState.musicVolume;
            musicSource.Play();
        }
    }
    
    private IEnumerator FadeToNewMusic(AudioClip newClip, float targetVolume)
    {
        float halfFade = fadeDuration / 2f;
        
        if (musicSource.isPlaying)
        {
            float startVolume = musicSource.volume;
            float elapsed = 0f;
            while (elapsed < halfFade)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / halfFade);
                yield return null;
            }
        }
        
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();
        
        float elapsed2 = 0f;
        while (elapsed2 < halfFade)
        {
            elapsed2 += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed2 / halfFade);
            yield return null;
        }
        
        musicSource.volume = targetVolume;
        musicFadeCoroutine = null;
    }
    
    private void SyncCameraPositions(CutsceneState state)
    {
        if (state.timeline == null) return;
        
        var cinemachineTracks = state.timeline.playableAsset.outputs;
        foreach (var output in cinemachineTracks)
        {
            if (output.sourceObject != null && output.outputTargetType == typeof(UnityEngine.Camera))
            {
                if (output.sourceObject.name.Contains("Cinemachine"))
                    return;
            }
        }
    }
    
    private void OnTimelineStopped(PlayableDirector director)
    {
        director.stopped -= OnTimelineStopped;
        Debug.Log($"[CutsceneChief] Timeline stopped for state {currentState}");
        
        if (shouldAutoAdvanceOnTimelineStop)
            AdvanceState();
        else
            Debug.Log("[CutsceneChief] Auto-advance disabled");
    }
    
    public void AdvanceState()
    {
        Debug.Log($"[CutsceneChief] Advancing from state {currentState}...");
        
        currentState++;
        
        if (currentState < cutsceneStates.Length)
        {
            SaveState();
            UpdateDebugUI();
            CutsceneState nextState = cutsceneStates[currentState];
            
            if (nextState.changeScene && !string.IsNullOrEmpty(nextState.targetSceneName))
            {
                Debug.Log($"[CutsceneChief] Scene change requested: {nextState.targetSceneName}");
                LoadingManager.LoadScene(nextState.targetSceneName, currentState, nextState.spawnPointName);
            }
            else
            {
                ShowGameSavedUI();
                PlayCurrentState();
            }
        }
        else
        {
            Debug.Log("[CutsceneChief] All states completed!");
        }
    }
    
    public void SetState(int newState)
    {
        if (newState < 0 || newState >= cutsceneStates.Length)
        {
            Debug.LogError($"[CutsceneChief] Cannot set invalid state: {newState}");
            return;
        }
        
        currentState = newState;
        SaveState();
        UpdateDebugUI();
        
        CutsceneState targetState = cutsceneStates[currentState];
        
        if (targetState.changeScene && !string.IsNullOrEmpty(targetState.targetSceneName))
            LoadingManager.LoadScene(targetState.targetSceneName, currentState, targetState.spawnPointName);
        else
        {
            ShowGameSavedUI();
            PlayCurrentState();
        }
    }
    
    private void SaveState()
    {
        PlayerPrefs.SetInt(saveKey, currentState);
        PlayerPrefs.Save();
        Debug.Log($"[CutsceneChief] 💾 State saved: {currentState}");
    }
    
    public void LoadState()
    {
        if (PlayerPrefs.HasKey(saveKey))
        {
            currentState = PlayerPrefs.GetInt(saveKey);
            Debug.Log($"[CutsceneChief] State loaded from save: {currentState}");
        }
        else
        {
            Debug.Log($"[CutsceneChief] No saved state, using Inspector value: {currentState}");
            SaveState();
        }
    }
    
    private void ShowGameSavedUI()
    {
        if (gameSavedCanvas == null)
        {
            GameObject foundCanvas = GameObject.Find("GameSaved");
            if (foundCanvas != null)
                gameSavedCanvas = foundCanvas;
            else
            {
                Debug.LogError("[CutsceneChief] ❌ GameSaved Canvas not found!");
                return;
            }
        }
        
        if (saveUICoroutine != null)
            StopCoroutine(saveUICoroutine);
        
        saveUICoroutine = StartCoroutine(GameSavedUISequence());
    }
    
    private IEnumerator GameSavedUISequence()
    {
        yield return new WaitForSeconds(1f);
        if (gameSavedCanvas != null) gameSavedCanvas.SetActive(true);
        yield return new WaitForSeconds(2f);
        if (gameSavedCanvas != null) gameSavedCanvas.SetActive(false);
        saveUICoroutine = null;
    }
    
    public void DisableAutoAdvance()
    {
        shouldAutoAdvanceOnTimelineStop = false;
        Debug.Log("[CutsceneChief] 🔒 Auto-advance DISABLED");
    }

    public void EnableAutoAdvance()
    {
        shouldAutoAdvanceOnTimelineStop = true;
        Debug.Log("[CutsceneChief] 🔓 Auto-advance ENABLED");
    }
    
    [ContextMenu("Reset State to 0")]
    public void ResetState()
    {
        currentState = 0;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        UpdateDebugUI();
        Debug.Log("[CutsceneChief] State reset to 0");
    }
    
    [ContextMenu("Advance State (Debug)")]
    public void DebugAdvanceState() { AdvanceState(); }
    
    [ContextMenu("Go to State 1")]
    public void GoToState1() { SetState(1); }
    
    [ContextMenu("Go to State 2")]
    public void GoToState2() { SetState(2); }
    
    [ContextMenu("Go to State 3")]
    public void GoToState3() { SetState(3); }
    
    [ContextMenu("Go to End Credits (State 21)")]
    public void GoToEndCredits() { SetState(21); }
    
    private void Update()
    {
        #if UNITY_EDITOR
        
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (currentState >= 0 && currentState < cutsceneStates.Length)
            {
                CutsceneState state = cutsceneStates[currentState];
                if (state.timeline != null && state.timeline.state == PlayState.Playing)
                {
                    state.timeline.Stop();
                    state.timeline.stopped -= OnTimelineStopped;
                }
            }
            AdvanceState();
        }
        
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (currentState > 0)
            {
                if (currentState >= 0 && currentState < cutsceneStates.Length)
                {
                    CutsceneState currentStateObj = cutsceneStates[currentState];
                    if (currentStateObj.timeline != null && currentStateObj.timeline.state == PlayState.Playing)
                    {
                        currentStateObj.timeline.Stop();
                        currentStateObj.timeline.stopped -= OnTimelineStopped;
                    }
                }
                
                currentState--;
                SaveState();
                UpdateDebugUI();
                
                CutsceneState targetState = cutsceneStates[currentState];
                
                if (targetState.changeScene && !string.IsNullOrEmpty(targetState.targetSceneName))
                    LoadingManager.LoadScene(targetState.targetSceneName, currentState, targetState.spawnPointName);
                else
                    PlayCurrentState();
            }
            else
            {
                Debug.LogWarning("[CutsceneChief] [DEBUG] Already at state 0, cannot go back");
            }
        }
        
        #endif
    }
    
    private void OnDestroy()
    {
        if (musicFadeCoroutine != null) { StopCoroutine(musicFadeCoroutine); musicFadeCoroutine = null; }
        if (saveUICoroutine != null) { StopCoroutine(saveUICoroutine); saveUICoroutine = null; }
        if (endCreditsCoroutine != null) { StopCoroutine(endCreditsCoroutine); endCreditsCoroutine = null; }
        
        if (currentState >= 0 && currentState < cutsceneStates.Length)
        {
            CutsceneState state = cutsceneStates[currentState];
            if (state.timeline != null)
                state.timeline.stopped -= OnTimelineStopped;
        }
        
        Debug.Log("[CutsceneChief] Cleaned up on destroy");
    }
}