using UnityEngine;
using System.Collections;
using UnityEngine.Playables;
using TMPro;

public class ActGateII : MonoBehaviour
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
    [SerializeField] private float dialogShowDelay = 1f; // UI kaç saniye sonra gözüksün
    [SerializeField] private float dialogueDuration = 3f;

    [Header("Dialog SFX")]
    [SerializeField] private AudioClip dialogSfx;

    [Header("Door Sound")]
    [SerializeField] private AudioClip doorSound;
    [SerializeField] private float doorSoundDuration = 1f;

    [Header("Black Screen")]
    [SerializeField] private float blackScreenDelay = 0.5f;

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
        Debug.Log($"[ActGateII] Initialized at {transform.position}");

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
            Debug.Log("[ActGateII] PlayableDirector found and stopped");
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[ActGateII] No spawn point assigned!");
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

        Debug.Log($"[ActGateII] 🚪 Gate triggered! Starting sequence...");

        StartCoroutine(GateSequence());
    }

    private IEnumerator GateSequence()
    {
        Debug.Log("[ActGateII] ========== SEQUENCE START ==========");

        // 1. Player'ı dondur
        if (playerController != null)
        {
            playerController.FreezePlayer();
            Debug.Log("[ActGateII] ✅ Player frozen");
        }

        // 2. State ilerlet
        if (cutsceneChief != null)
        {
            Debug.Log("[ActGateII] ⏩ Advancing state...");
            cutsceneChief.DisableAutoAdvance();
            cutsceneChief.AdvanceState();
            Debug.Log("[ActGateII] ✅ State advanced");
        }

        // 3. Timeline oynat (varsa)
        if (playableDirector != null)
        {
            Debug.Log("[ActGateII] 🎬 Playing Timeline...");
            playableDirector.Play();

            // Timeline oynarken AYNI ANDA dialog göster (delay ile)
            StartCoroutine(DelayedDialogShow());

            // Timeline bitene kadar bekle
            float waitTime = 0f;
            while (playableDirector.state == PlayState.Playing)
            {
                waitTime += Time.deltaTime;
                yield return null;
            }

            Debug.Log($"[ActGateII] ✅ Timeline finished after {waitTime:F2} seconds");
            
            // Timeline bitti, dialog'u gizle
            HideDialog();
            Debug.Log("[ActGateII] ✅ Dialog hidden after timeline");
        }
        else
        {
            // Timeline yoksa direkt dialog göster
            yield return new WaitForSeconds(dialogShowDelay);
            ShowDialog(npcDialogue);
            
            if (audioSource != null && dialogSfx != null)
            {
                audioSource.PlayOneShot(dialogSfx);
            }
            
            yield return new WaitForSeconds(dialogueDuration);
            HideDialog();
        }

        // 4. Black screen
        Debug.Log("[ActGateII] ⚫ Black screen ON");
        ShowBlackScreen();

        Debug.Log("[ActGateII] ⏱️ Waiting 2 seconds in darkness...");
        yield return new WaitForSeconds(2f);

        // 5. Kapı sesi çal
        if (doorSound != null && audioSource != null)
        {
            Debug.Log("[ActGateII] 🔊 Playing door sound");
            audioSource.PlayOneShot(doorSound);
        }

        yield return new WaitForSeconds(doorSoundDuration + blackScreenDelay);

        // 6. Player'ı teleport et
        if (player != null && spawnPoint != null)
        {
            Debug.Log($"[ActGateII] 📍 Teleporting player to: {spawnPoint.position}");
            player.position = spawnPoint.position;
        }

        // 7. Gameplay state'ine geç
        if (cutsceneChief != null)
        {
            Debug.Log("[ActGateII] ⏩ Advancing to gameplay state...");
            cutsceneChief.EnableAutoAdvance();
            cutsceneChief.AdvanceState();
            Debug.Log("[ActGateII] ✅ State advanced to gameplay");
        }

        // 8. Ekranı aç
        Debug.Log("[ActGateII] ⚪ Black screen OFF - BAM!");
        HideBlackScreen();

        // 9. Player'ı çöz
        if (playerController != null)
        {
            playerController.UnfreezePlayer();
            Debug.Log("[ActGateII] ✅ Player unfrozen");
        }

        Debug.Log("[ActGateII] ========== SEQUENCE END ==========");
    }

    private IEnumerator DelayedDialogShow()
    {
        Debug.Log($"[ActGateII] ⏱️ Waiting {dialogShowDelay}s before showing dialog...");
        yield return new WaitForSeconds(dialogShowDelay);

        ShowDialog(npcDialogue);

        if (audioSource != null && dialogSfx != null)
        {
            audioSource.PlayOneShot(dialogSfx);
        }

        Debug.Log("[ActGateII] ✅ Dialog shown!");
        
        // Dialog süresi kadar bekle, sonra otomatik gizle
        yield return new WaitForSeconds(dialogueDuration);
        
        HideDialog();
        Debug.Log($"[ActGateII] ✅ Dialog auto-hidden after {dialogueDuration}s");
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

        Debug.Log($"[ActGateII] 💬 NPC: {text}");
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
            blackScreen.SetActive(true);
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