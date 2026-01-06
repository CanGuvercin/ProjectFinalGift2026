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
        
        [Header("Music")]
        public AudioClip ambientMusic; // YENİ!
        [Range(0f, 1f)] public float musicVolume = 0.5f; // YENİ!
        public bool fadeMusic = true; // YENİ! (Smooth geçiş)
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
        LoadState();
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
        if (player != null && state.playerSpawnPosition != null)
        {
            player.transform.position = state.playerSpawnPosition.position;
            Debug.Log($"[State {currentState}] Player spawned at: {state.playerSpawnPosition.position}");
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
            PlayCurrentState();
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
        PlayCurrentState();
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
            Debug.Log($"[CutsceneChief] No saved state, starting at: {currentState}");
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