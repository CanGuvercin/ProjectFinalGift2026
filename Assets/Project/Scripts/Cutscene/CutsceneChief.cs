using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using System.Collections;

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
    
    // ═══ YENİ: SAHNE GÜVENLİK SİSTEMİ ═══
    [System.Serializable]
    public class SceneSafetyRule
    {
        public string sceneName;
        public int requiredState;
        public Vector3 emergencySpawnPosition;
        public bool strictMode = true; // true: Yanlış state'i zorla düzelt
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
        {
            gameSavedCanvas.SetActive(false);
        }
    }
    
    private void Start()
    {
        // ═══ YENİ: SAHNE GÜVENLİK KONTROLÜ ═══
        ValidateSceneSafety();
        
        int loadingState = LoadingManager.GetTargetState();
        
        if (loadingState >= 0)
        {
            // Bounds check ekle
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
        
        // State 21 (End Credits) kontrolü
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
    
    // ═══ YENİ: SAHNE GÜVENLİK VALİDASYONU ═══
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
                    Debug.LogWarning($"[CutsceneChief] ⚠️ STATE MISMATCH!");
                    Debug.LogWarning($"  Scene: {currentSceneName}");
                    Debug.LogWarning($"  Expected: State {rule.requiredState}");
                    Debug.LogWarning($"  Found: State {savedState}");
                    
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
        {
            StopCoroutine(endCreditsCoroutine);
        }
        
        endCreditsCoroutine = StartCoroutine(EndCreditsTimer());
    }
    
    private IEnumerator EndCreditsTimer()
    {
        yield return new WaitForSeconds(endCreditsDelay);
        
        Debug.Log("[CutsceneChief] ========== 🎬 GAME COMPLETED 🎬 ==========");
        Debug.Log("[CutsceneChief] Thank you for playing Farewell to my PLAYGROUND");
        
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log("[CutsceneChief] 🗑️ Game state deleted - Fresh start available");
        
        Debug.Log("[CutsceneChief] 🏠 Returning to Main Menu...");
        LoadingManager.LoadScene("MainMenu");
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
        
        HandleMusic(state);
        
        SyncCameraPositions(state);
        
        if (state.objectsToDeactivate != null)
        {
            foreach (GameObject obj in state.objectsToDeactivate)
            {
                if (obj != null)
                {
                    obj.SetActive(false);
                    Debug.Log($"[State {currentState}] Deactivated: {obj.name}");
                }
            }
        }
        
        if (state.objectsToActivate != null)
        {
            foreach (GameObject obj in state.objectsToActivate)
            {
                if (obj != null)
                {
                    obj.SetActive(true);
                    Debug.Log($"[State {currentState}] Activated: {obj.name}");
                }
            }
        }
        
        // ═══ GELİŞTİRİLMİŞ PLAYER SPAWN SİSTEMİ ═══
        SpawnPlayer(state);
        
        // ═══ FIX: Timeline Event Memory Leak ═══
        if (state.timeline != null)
        {
            // Önce mevcut subscription'ı temizle
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
    
    // ═══ YENİ: GÜVENLİ PLAYER SPAWN SİSTEMİ ═══
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
        
        // Öncelik 1: LoadingManager spawn point
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
        
        // Öncelik 2: State spawn position
        if (!spawnPosition.HasValue && state.playerSpawnPosition != null)
        {
            spawnPosition = state.playerSpawnPosition.position;
            spawnSource = "State Spawn Point";
        }
        
        // Öncelik 3: Emergency spawn (Scene Safety Rule)
        if (!spawnPosition.HasValue)
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
        
        // Final: Spawn player
        if (spawnPosition.HasValue)
        {
            player.transform.position = spawnPosition.Value;
            Debug.Log($"[State {currentState}] ✅ Player spawned at: {spawnSource} → {spawnPosition.Value}");
        }
        else
        {
            Debug.LogError($"[State {currentState}] ❌ CRITICAL: No spawn position available! Player at: {player.transform.position}");
        }
    }
    
    private void HandleMusic(CutsceneState newState)
    {
        if (musicSource == null)
        {
            Debug.LogWarning("[Music] MusicSource was null or destroyed, recreating...");
            GameObject musicObj = new GameObject("CutsceneMusic");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
        
        if (newState.ambientMusic == null)
        {
            Debug.Log($"[Music] No music defined for State {currentState}, continuing current music");
            return;
        }
        
        if (musicSource.clip == newState.ambientMusic && musicSource.isPlaying)
        {
            Debug.Log($"[Music] Same music already playing: {newState.ambientMusic.name}");
            return;
        }
        
        if (newState.fadeMusic)
        {
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            
            musicFadeCoroutine = StartCoroutine(FadeToNewMusic(newState.ambientMusic, newState.musicVolume));
        }
        else
        {
            musicSource.Stop();
            musicSource.clip = newState.ambientMusic;
            musicSource.volume = newState.musicVolume;
            musicSource.Play();
            Debug.Log($"[Music] Playing: {newState.ambientMusic.name} (no fade)");
        }
    }
    
    private IEnumerator FadeToNewMusic(AudioClip newClip, float targetVolume)
    {
        Debug.Log($"[Music] Fading to: {newClip.name}");
        
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
        
        Debug.Log($"[Music] Fade complete: {newClip.name}");
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
                {
                    return;
                }
            }
        }
    }
    
    private void OnTimelineStopped(PlayableDirector director)
    {
        director.stopped -= OnTimelineStopped;
        
        Debug.Log($"[CutsceneChief] Timeline stopped for state {currentState}");
        
        if (shouldAutoAdvanceOnTimelineStop)
        {
            Debug.Log("[CutsceneChief] Auto-advancing...");
            AdvanceState();
        }
        else
        {
            Debug.Log("[CutsceneChief] Auto-advance disabled");
        }
    }
    
    public void AdvanceState()
    {
        Debug.Log($"[CutsceneChief] Advancing from state {currentState}...");
        
        currentState++;
        
        if (currentState < cutsceneStates.Length)
        {
            SaveState();
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
        
        CutsceneState targetState = cutsceneStates[currentState];
        
        if (targetState.changeScene && !string.IsNullOrEmpty(targetState.targetSceneName))
        {
            Debug.Log($"[CutsceneChief] Scene change requested: {targetState.targetSceneName}");
            LoadingManager.LoadScene(targetState.targetSceneName, currentState, targetState.spawnPointName);
        }
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
        Debug.Log($"[CutsceneChief] ShowGameSavedUI called - Canvas null? {gameSavedCanvas == null}");
        
        if (gameSavedCanvas == null)
        {
            Debug.LogWarning("[CutsceneChief] ⚠️ GameSaved Canvas is NULL! Trying to find it...");
            
            GameObject foundCanvas = GameObject.Find("GameSaved");
            if (foundCanvas != null)
            {
                gameSavedCanvas = foundCanvas;
                Debug.Log("[CutsceneChief] ✅ Found GameSaved Canvas in scene!");
            }
            else
            {
                Debug.LogError("[CutsceneChief] ❌ GameSaved Canvas not found anywhere!");
                return;
            }
        }
        
        if (saveUICoroutine != null)
        {
            StopCoroutine(saveUICoroutine);
        }
        
        saveUICoroutine = StartCoroutine(GameSavedUISequence());
    }
    
    private IEnumerator GameSavedUISequence()
    {
        Debug.Log("[CutsceneChief] 🔄 Coroutine STARTED");
        
        yield return new WaitForSeconds(1f);
        
        Debug.Log($"[CutsceneChief] 🔍 gameSavedCanvas null? {gameSavedCanvas == null}");
        
        if (gameSavedCanvas != null)
        {
            gameSavedCanvas.SetActive(true);
            Debug.Log($"[CutsceneChief] 💾 Game Saved UI shown - Active: {gameSavedCanvas.activeSelf}");
        }
        
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[CutsceneChief] ⏰ 2 seconds passed, closing now...");
        
        if (gameSavedCanvas != null)
        {
            gameSavedCanvas.SetActive(false);
            Debug.Log($"[CutsceneChief] 💾 Game Saved UI hidden - Active: {gameSavedCanvas.activeSelf}");
        }
        
        saveUICoroutine = null;
        Debug.Log("[CutsceneChief] ✅ Coroutine FINISHED");
    }
    
    // ═══ AUTO-ADVANCE CONTROL (ActGate için) ═══
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
        Debug.Log("[CutsceneChief] State reset to 0 (save deleted)");
    }
    
    [ContextMenu("Advance State (Debug)")]
    public void DebugAdvanceState()
    {
        AdvanceState();
    }
    
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
            Debug.Log($"[CutsceneChief] [DEBUG] Key 1: Advancing to next state");
            
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
                Debug.Log($"[CutsceneChief] [DEBUG] Key 2: Going to previous state");
                
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
                
                Debug.Log($"[CutsceneChief] Now at state: {currentState}");
                
                CutsceneState targetState = cutsceneStates[currentState];
                
                if (targetState.changeScene && !string.IsNullOrEmpty(targetState.targetSceneName))
                {
                    Debug.Log($"[CutsceneChief] Previous state requires scene: {targetState.targetSceneName}");
                    LoadingManager.LoadScene(targetState.targetSceneName, currentState, targetState.spawnPointName);
                }
                else
                {
                    PlayCurrentState();
                }
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
        if (musicFadeCoroutine != null)
        {
            StopCoroutine(musicFadeCoroutine);
            musicFadeCoroutine = null;
        }
        
        if (saveUICoroutine != null)
        {
            StopCoroutine(saveUICoroutine);
            saveUICoroutine = null;
        }
        
        if (endCreditsCoroutine != null)
        {
            StopCoroutine(endCreditsCoroutine);
            endCreditsCoroutine = null;
        }
        
        if (currentState >= 0 && currentState < cutsceneStates.Length)
        {
            CutsceneState state = cutsceneStates[currentState];
            if (state.timeline != null)
            {
                state.timeline.stopped -= OnTimelineStopped;
            }
        }
        
        Debug.Log("[CutsceneChief] Cleaned up on destroy");
    }
}