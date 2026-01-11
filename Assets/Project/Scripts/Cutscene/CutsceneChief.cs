using UnityEngine;
using UnityEngine.Playables;

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
        [Tooltip("Bu state'e geçerken scene değişsin mi?")]
        public string targetSceneName = "";
        [Tooltip("Yüklenecek scene adı (örn: Dungeon1)")]
        public string spawnPointName = "";
        [Tooltip("Yeni scene'de spawn noktası (örn: DungeonEntrance)")]
        
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
    
    private Coroutine musicFadeCoroutine;
    
    private void Awake()
    {
        // Music AudioSource yoksa oluştur
        if (musicSource == null)
        {
            GameObject musicObj = new GameObject("CutsceneMusic");
            musicObj.transform.SetParent(transform);
            musicSource = musicObj.AddComponent<AudioSource>();
            musicSource.loop = true;
            musicSource.playOnAwake = false;
        }
    }
    
    private void Start()
    {
        // LoadingManager'dan gelen state var mı kontrol et
        int loadingState = LoadingManager.GetTargetState();
        
        if (loadingState >= 0)
        {
            // LoadingManager'dan state geldi, onu kullan
            Debug.Log($"[CutsceneChief] State override from LoadingManager: {loadingState}");
            currentState = loadingState;
            SaveState();
            
            // LoadingManager'ı temizle (okuduğumuzu bildiriyoruz)
            LoadingManager.ClearTransitionData();
        }
        else
        {
            // Normal save'den yükle
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
        
        // Müziği kontrol et
        HandleMusic(state);
        
        // Kamera pozisyonunu senkronize et
        SyncCameraPositions(state);
        
        // Deactivate
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
        
        // Activate
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
        
        // Player spawn
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            // Önce LoadingManager'dan gelen spawn point'i kontrol et
            string spawnPointName = LoadingManager.GetSpawnPoint();
            
            if (!string.IsNullOrEmpty(spawnPointName))
            {
                // LoadingManager'dan spawn point gelmiş (örn: dungeon girişi)
                GameObject spawnPoint = GameObject.Find(spawnPointName);
                if (spawnPoint != null)
                {
                    player.transform.position = spawnPoint.transform.position;
                    Debug.Log($"[State {currentState}] Player spawned at LoadingManager point: {spawnPointName} ({spawnPoint.transform.position})");
                }
                else
                {
                    Debug.LogWarning($"[State {currentState}] Spawn point '{spawnPointName}' not found! Using state spawn.");
                    
                    // Bulamazsa state'in spawn'ını kullan
                    if (state.playerSpawnPosition != null)
                    {
                        player.transform.position = state.playerSpawnPosition.position;
                        Debug.Log($"[State {currentState}] Fallback: Player spawned at state spawn: {state.playerSpawnPosition.position}");
                    }
                }
            }
            else if (state.playerSpawnPosition != null)
            {
                // Normal spawn (LoadingManager'dan gelen yok)
                player.transform.position = state.playerSpawnPosition.position;
                Debug.Log($"[State {currentState}] Player spawned at state position: {state.playerSpawnPosition.position}");
            }
        }
        
        // Timeline
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
        // Müzik slotu boş → Devam et (değişiklik yok)
        if (newState.ambientMusic == null)
        {
            Debug.Log($"[Music] No music defined for State {currentState}, continuing current music");
            return;
        }
        
        // Aynı müzik çalıyor → Devam et
        if (musicSource.clip == newState.ambientMusic && musicSource.isPlaying)
        {
            Debug.Log($"[Music] Same music already playing: {newState.ambientMusic.name}");
            return;
        }
        
        // Yeni müzik çal
        if (newState.fadeMusic)
        {
            // Fade ile geçiş
            if (musicFadeCoroutine != null)
            {
                StopCoroutine(musicFadeCoroutine);
            }
            musicFadeCoroutine = StartCoroutine(FadeToNewMusic(newState.ambientMusic, newState.musicVolume));
        }
        else
        {
            // Anında geçiş
            musicSource.clip = newState.ambientMusic;
            musicSource.volume = newState.musicVolume;
            musicSource.Play();
            Debug.Log($"[Music] Playing: {newState.ambientMusic.name}");
        }
    }
    
    private System.Collections.IEnumerator FadeToNewMusic(AudioClip newClip, float targetVolume)
    {
        // Fade out (mevcut müzik)
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
        
        // Yeni müziği başlat
        musicSource.clip = newClip;
        musicSource.volume = 0f;
        musicSource.Play();
        Debug.Log($"[Music] Fading to: {newClip.name}");
        
        // Fade in (yeni müzik)
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
            Debug.Log($"[CutsceneChief] Camera synced: {activeCamera.name} → {nextCamera.name} at {nextCamera.transform.position}");
        }
    }
    
    private void OnTimelineStopped(PlayableDirector director)
    {
        Debug.Log($"[CutsceneChief] Timeline finished: {director.name}");
        director.stopped -= OnTimelineStopped;
        AdvanceState();
    }
    
    public void AdvanceState()
    {
        currentState++;
        SaveState();
        
        Debug.Log($"[CutsceneChief] ======= Advanced to state: {currentState} =======");
        
        if (currentState < cutsceneStates.Length)
        {
            CutsceneState nextState = cutsceneStates[currentState];
            
            // Scene değişikliği var mı?
            if (nextState.changeScene && !string.IsNullOrEmpty(nextState.targetSceneName))
            {
                Debug.Log($"[CutsceneChief] 🌍 Scene change requested: {nextState.targetSceneName}");
                Debug.Log($"[CutsceneChief] State: {currentState}, Spawn: {nextState.spawnPointName}");
                
                // LoadingManager ile scene yükle
                LoadingManager.LoadScene(nextState.targetSceneName, currentState, nextState.spawnPointName);
            }
            else
            {
                // Normal state geçişi (aynı scene içinde)
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
        
        // Scene değişikliği var mı?
        if (targetState.changeScene && !string.IsNullOrEmpty(targetState.targetSceneName))
        {
            Debug.Log($"[CutsceneChief] 🌍 Scene change requested: {targetState.targetSceneName}");
            Debug.Log($"[CutsceneChief] State: {currentState}, Spawn: {targetState.spawnPointName}");
            
            // LoadingManager ile scene yükle
            LoadingManager.LoadScene(targetState.targetSceneName, currentState, targetState.spawnPointName);
        }
        else
        {
            // Normal state geçişi (aynı scene içinde)
            PlayCurrentState();
        }
    }
    
    private void SaveState()
    {
        PlayerPrefs.SetInt(saveKey, currentState);
        PlayerPrefs.Save();
        Debug.Log($"[CutsceneChief] State saved: {currentState}");
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
        // YENI: Inspector değerini hemen kaydet!
        SaveState(); // ← EKLE!
    }
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
        if (currentState >= 0 && currentState < cutsceneStates.Length)
        {
            CutsceneState state = cutsceneStates[currentState];
            
            if (state.timeline != null && state.timeline.state == PlayState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    Debug.Log($"[CutsceneChief] Skipping State {currentState} with ESC");
                    state.timeline.Stop();
                    OnTimelineStopped(state.timeline);
                }
            }
        }
    }
}