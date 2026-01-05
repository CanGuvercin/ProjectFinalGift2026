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
    }
    
    [Header("Cutscene States")]
    [SerializeField] private CutsceneState[] cutsceneStates;
    
    [Header("Current State")]
    [SerializeField] private int currentState = 0;
    
    [Header("Save Key")]
    [SerializeField] private string saveKey = "GameState";
    
    private void Start()
    {
        // Kayıtlı state'i yükle
        LoadState();
        
        // Mevcut state'i oynat
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
        
        // Kamera pozisyonunu senkronize et (ÖNCE!)
        SyncCameraPositions(state);
        
        // ÖNCE DEACTIVATE (önceki state'in objelerini kapat)
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
        
        // SONRA ACTIVATE (bu state'in objelerini aç)
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
        
        // Player spawn (eğer varsa)
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && state.playerSpawnPosition != null)
        {
            player.transform.position = state.playerSpawnPosition.position;
            Debug.Log($"[State {currentState}] Player spawned at: {state.playerSpawnPosition.position}");
        }
        
        // Timeline varsa oynat
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
    
    private void SyncCameraPositions(CutsceneState nextState)
    {
        // Aktif kamerayı bul
        Camera activeCamera = Camera.main;
        if (activeCamera == null)
        {
            // Fallback: İlk aktif kamerayı bul
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
        
        // Yeni state'in kamerasını bul
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
        
        // Pozisyonu kopyala
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
        
        // Event'i temizle
        director.stopped -= OnTimelineStopped;
        
        // Bir sonraki state'e geç
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
    
    // Debug: State'i sıfırla
    [ContextMenu("Reset State to 0")]
    public void ResetState()
    {
        currentState = 0;
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();
        Debug.Log("[CutsceneChief] State reset to 0 (save deleted)");
    }
    
    // Debug: Bir sonraki state
    [ContextMenu("Advance State (Debug)")]
    public void DebugAdvanceState()
    {
        AdvanceState();
    }
    
    // Debug: Belirli state'e git
    [ContextMenu("Go to State 1")]
    public void GoToState1() { SetState(1); }
    
    [ContextMenu("Go to State 2")]
    public void GoToState2() { SetState(2); }
    
    [ContextMenu("Go to State 3")]
    public void GoToState3() { SetState(3); }
    
    // ESC ile skip
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