using UnityEngine;
using UnityEngine.Playables;
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
    
    private Coroutine musicFadeCoroutine;
    private Coroutine saveUICoroutine;
    
    // 👇 YENİ: Timeline bitince otomatik state atlama kontrolü
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
        int loadingState = LoadingManager.GetTargetState();
        
        if (loadingState >= 0)
        {
            Debug.Log($"[CutsceneChief] State override from LoadingManager: {loadingState}");
            currentState = loadingState;
            SaveState();
            
            LoadingManager.ClearTransitionData();
        }
        else
        {
            LoadState();
        }
        
        PlayCurrentState();
    }
    
    private void PlayCurrentState()
    {
        if (currentState < 0 || currentState >= cutsceneStates.Length)
        {
            Debug.LogError($"[CutsceneChief] Invalid state: {currentState}");
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
        
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            string spawnPointName = LoadingManager.GetSpawnPoint();
            
            if (!string.IsNullOrEmpty(spawnPointName))
            {
                GameObject spawnPoint = GameObject.Find(spawnPointName);
                if (spawnPoint != null)
                {
                    player.transform.position = spawnPoint.transform.position;
                    Debug.Log($"[State {currentState}] Player spawned at LoadingManager point: {spawnPointName}");
                }
                else
                {
                    Debug.LogWarning($"[State {currentState}] Spawn point '{spawnPointName}' not found!");
                    
                    if (state.playerSpawnPosition != null)
                    {
                        player.transform.position = state.playerSpawnPosition.position;
                        Debug.Log($"[State {currentState}] Fallback: Player spawned at state spawn");
                    }
                }
            }
            else if (state.playerSpawnPosition != null)
            {
                player.transform.position = state.playerSpawnPosition.position;
                Debug.Log($"[State {currentState}] Player spawned at state position");
            }
        }
        
        if (state.timeline != null)
        {
            state.timeline.Play();
            state.timeline.stopped += OnTimelineStopped;
            Debug.Log($"[State {currentState}] Timeline started: {state.timeline.name}");
        }
        else
        {
            Debug.Log($"[State {currentState}] No timeline, state ready");
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
            musicSource.clip = newState.ambientMusic;
            musicSource.volume = newState.musicVolume;
            musicSource.Play();
            Debug.Log($"[Music] Playing: {newState.ambientMusic.name}");
        }
    }
    
    private System.Collections.IEnumerator FadeToNewMusic(AudioClip newClip, float targetVolume)
    {
        float startVolume = musicSource.volume;
        
        if (musicSource.isPlaying)
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration / 2f)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / (fadeDuration / 2f));
                yield return null;
            }
        }
        
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();
        Debug.Log($"[Music] Fading to: {newClip.name}");
        
        float elapsed2 = 0f;
        while (elapsed2 < fadeDuration / 2f)
        {
            elapsed2 += Time.deltaTime;
            musicSource.volume = Mathf.Lerp(0f, targetVolume, elapsed2 / (fadeDuration / 2f));
            yield return null;
        }
        
        musicSource.volume = targetVolume;
    }
    
    private void SyncCameraPositions(CutsceneState nextState)
    {
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            Camera[] allCameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in allCameras)
            {
                if (cam.enabled)
                {
                    activeCamera = cam;
                    break;
                }
            }
        }
        
        Camera nextCamera = null;
        foreach (GameObject obj in nextState.objectsToActivate)
        {
            if (obj != null)
            {
                Camera cam = obj.GetComponentInChildren<Camera>(true);
                if (cam != null)
                {
                    nextCamera = cam;
                    break;
                }
            }
        }
        
        if (activeCamera != null && nextCamera != null && activeCamera != nextCamera)
        {
            nextCamera.transform.position = activeCamera.transform.position;
            nextCamera.transform.rotation = activeCamera.transform.rotation;
            Debug.Log($"[CutsceneChief] Camera synced: {activeCamera.name} -> {nextCamera.name}");
        }
    }
    
    // 👇 DEĞİŞTİ: Otomatik atlama kontrolü eklendi
    private void OnTimelineStopped(PlayableDirector director)
    {
        Debug.Log($"[CutsceneChief] Timeline finished: {director.name}");
        director.stopped -= OnTimelineStopped;
        
        // SADECE flag true ise otomatik state ilerlet
        if (shouldAutoAdvanceOnTimelineStop)
        {
            Debug.Log("[CutsceneChief] Auto-advancing state after timeline");
            AdvanceState();
        }
        else
        {
            Debug.Log("[CutsceneChief] Auto-advance disabled, skipping state change");
        }
    }
    
    // 👇 YENİ: Otomatik atlama kontrolü
    public void DisableAutoAdvance()
    {
        shouldAutoAdvanceOnTimelineStop = false;
        Debug.Log("[CutsceneChief] ⛔ Auto-advance DISABLED");
    }
    
    public void EnableAutoAdvance()
    {
        shouldAutoAdvanceOnTimelineStop = true;
        Debug.Log("[CutsceneChief] ✅ Auto-advance ENABLED");
    }
    
    public void AdvanceState()
    {
        currentState++;
        SaveState();
        
        Debug.Log($"[CutsceneChief] ======= Advanced to state: {currentState} =======");
        
        if (currentState < cutsceneStates.Length)
        {
            CutsceneState nextState = cutsceneStates[currentState];
            
            if (nextState.changeScene && !string.IsNullOrEmpty(nextState.targetSceneName))
            {
                Debug.Log($"[CutsceneChief] Scene change requested: {nextState.targetSceneName}");
                
                ShowGameSavedUI();
                
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
            
            ShowGameSavedUI();
            
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
    
    private void LoadState()
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
        
        // Sahnede varsa bul
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