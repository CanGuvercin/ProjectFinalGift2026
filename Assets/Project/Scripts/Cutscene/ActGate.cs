using UnityEngine;
using System.Collections;
using UnityEngine.Playables;
using TMPro;

public class ActGate : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 2f;
    [SerializeField] private bool useManualActivation = false;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector playableDirector;

    [Header("Dialog UI")]
    [SerializeField] private GameObject dialogCanvas;
    [SerializeField] private TextMeshProUGUI dialogText;

    [Header("Dialog Content")]
    [TextArea(2, 3)]
    [SerializeField] private string npcDialogue = "İyi iş Ali! Şimdi diğer zindanı bulmalı ve ileri kapıyı açmalısın!";
    [SerializeField] private float dialogueDuration = 3f;

    [Header("Dialog SFX")]
    [SerializeField] private AudioClip dialogSfx;

    [Header("Door Sound")]
    [SerializeField] private AudioClip doorSound;
    [SerializeField] private float doorSoundDuration = 1f; // Kapı sesi ne kadar sürsün

    [Header("Black Screen")]
    [SerializeField] private float blackScreenDelay = 0.5f; // Kapı sesinden sonra ne kadar beklesin

    [Header("Teleport")]
    [SerializeField] private Transform spawnPoint;

    [Header("References")]
    [SerializeField] private CutsceneChief cutsceneChief;
    [SerializeField] private GameObject promptUI;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private Transform player;
    private PlayerController playerController;
    private bool isPlayerNear;
    private bool hasBeenActivated;
    private GameObject blackScreen;

    private void Start()
    {
        Debug.Log($"[ActGate] Initialized at {transform.position}");

        if (cutsceneChief == null)
        {
            cutsceneChief = FindObjectOfType<CutsceneChief>();
        }

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        CreateBlackScreen();

        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }

        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }

        if (playableDirector != null)
        {
            playableDirector.Stop();
            Debug.Log("[ActGate] PlayableDirector found and stopped");
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[ActGate] No spawn point assigned!");
        }
    }

    private void Update()
    {
        if (hasBeenActivated) return;

        CheckPlayerProximity();

        if (useManualActivation)
        {
            if (isPlayerNear && Input.GetKeyDown(KeyCode.E))
            {
                TriggerGate();
            }
        }
        else
        {
            if (isPlayerNear)
            {
                TriggerGate();
            }
        }
    }

    private void CheckPlayerProximity()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                playerController = player.GetComponent<PlayerController>();
            }
            else
            {
                return;
            }
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool wasNear = isPlayerNear;
        isPlayerNear = distance <= activationRadius;

        if (useManualActivation && isPlayerNear != wasNear && promptUI != null)
        {
            promptUI.SetActive(isPlayerNear);
        }
    }

    private void TriggerGate()
    {
        if (hasBeenActivated) return;

        hasBeenActivated = true;
        if (promptUI != null) promptUI.SetActive(false);

        Debug.Log($"[ActGate] 🚪 Gate triggered! Starting sequence...");

        StartCoroutine(GateSequence());
    }

    private IEnumerator GateSequence()
{
    Debug.Log("[ActGate] ========== SEQUENCE START ==========");

    // 1. Player'ı dondur
    if (playerController != null)
    {
        playerController.FreezePlayer();
        Debug.Log("[ActGate] ✅ Step 1: Player frozen");
    }

    // 2. State ilerlet
    if (cutsceneChief != null)
    {
        Debug.Log("[ActGate] ⏩ Step 2: Advancing state...");
        cutsceneChief.DisableAutoAdvance();
        cutsceneChief.AdvanceState();
        Debug.Log("[ActGate] ✅ Step 2: State advanced");
    }

    // 3. Timeline
    Debug.Log("[ActGate] ⏸️ Step 3: About to play timeline...");

    if (playableDirector != null)
    {
        Debug.Log($"[ActGate] Timeline state BEFORE play: {playableDirector.state}");
        Debug.Log($"[ActGate] Timeline time BEFORE play: {playableDirector.time}");

        playableDirector.Play();

        Debug.Log($"[ActGate] ✅ Timeline.Play() called!");
        Debug.Log($"[ActGate] Timeline state AFTER play: {playableDirector.state}");

        float waitTime = 0f;
        while (playableDirector.state == PlayState.Playing)
        {
            waitTime += Time.deltaTime;
            yield return null;
        }

        Debug.Log($"[ActGate] ✅ Timeline finished after {waitTime:F2} seconds");
    }
    else
    {
        Debug.LogWarning("[ActGate] ⚠️ No PlayableDirector assigned!");
    }

    // 4. Dialog
    Debug.Log("[ActGate] ⏸️ Step 4: About to show dialog...");

    ShowDialog(npcDialogue);

    if (audioSource != null && dialogSfx != null)
    {
        audioSource.PlayOneShot(dialogSfx);
    }

    Debug.Log($"[ActGate] ✅ Dialog shown, waiting {dialogueDuration}s...");
    yield return new WaitForSeconds(dialogueDuration);

    HideDialog();
    Debug.Log("[ActGate] ✅ Dialog hidden");

    // 5. Black screen
    Debug.Log("[ActGate] ⏸️ Step 5: Showing black screen...");
    ShowBlackScreen();

    Debug.Log("[ActGate] ⏱️ Waiting 2 seconds in darkness...");
    yield return new WaitForSeconds(2f);

    // 6. Kapı sesi çal
    if (doorSound != null && audioSource != null)
    {
        Debug.Log("[ActGate] 🔊 Playing door sound");
        audioSource.PlayOneShot(doorSound);
    }

    // 7. Kapı sesi + delay
    Debug.Log($"[ActGate] ⏱️ Waiting {doorSoundDuration + blackScreenDelay}s for door sound...");
    yield return new WaitForSeconds(doorSoundDuration + blackScreenDelay);

    // 8. Player'ı teleport et
    if (player != null && spawnPoint != null)
    {
        Debug.Log($"[ActGate] 📍 Teleporting player to: {spawnPoint.position}");
        player.position = spawnPoint.position;
    }
    else
    {
        Debug.LogWarning("[ActGate] ⚠️ Cannot teleport - player or spawnPoint is null!");
    }

    // 9. ŞİMDİ gameplay state'ine geç (10 → 11)
    if (cutsceneChief != null)
    {
        Debug.Log("[ActGate] ⏩ Step 6: Advancing to gameplay state...");
        cutsceneChief.EnableAutoAdvance();
        cutsceneChief.AdvanceState();
        Debug.Log("[ActGate] ✅ Step 6: State advanced to gameplay");
    }

    // 10. Ekranı aç - BAM!
    Debug.Log("[ActGate] ⚪ Black screen OFF - BAM!");
    HideBlackScreen();

    // 11. Player'ı çöz
    if (playerController != null)
    {
        playerController.UnfreezePlayer();
        Debug.Log("[ActGate] ✅ Step 7: Player unfrozen");
    }

    Debug.Log("[ActGate] ========== SEQUENCE END ==========");
}
    #region Dialog System

    private void ShowDialog(string text)
    {
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(true);
        }

        if (dialogText != null)
        {
            dialogText.text = text;
        }

        Debug.Log($"[ActGate] 💬 NPC: {text}");
    }

    private void HideDialog()
    {
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }
    }

    #endregion

    #region Black Screen System

    private void CreateBlackScreen()
    {
        // Siyah ekran oluştur
        GameObject screenObj = new GameObject("BlackScreen");
        screenObj.transform.SetParent(transform);

        Canvas canvas = screenObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(screenObj.transform, false);

        UnityEngine.UI.Image image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        blackScreen = screenObj;
        blackScreen.SetActive(false);
    }

    private void ShowBlackScreen()
    {
        if (blackScreen != null)
        {
            Debug.Log("[ActGate] 🟥 ACTIVATING BLACK SCREEN OBJECT");
            blackScreen.SetActive(true);

            // Canvas'ı kontrol et
            Canvas canvas = blackScreen.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[ActGate] Canvas sortingOrder: {canvas.sortingOrder}");
            }
        }
        else
        {
            Debug.LogError("[ActGate] ❌ BLACK SCREEN IS NULL!");
        }
    }
    private void HideBlackScreen()
    {
        if (blackScreen != null)
        {
            blackScreen.SetActive(false);
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);

            Vector3 direction = (spawnPoint.position - transform.position).normalized;
            Gizmos.DrawRay(transform.position, direction * activationRadius);
        }
    }
}