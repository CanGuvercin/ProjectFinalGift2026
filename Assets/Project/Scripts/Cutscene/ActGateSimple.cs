using UnityEngine;
using System.Collections;
using TMPro;

public class ActGateSimple : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 2f;

    [Header("Message UI")]//.
    [SerializeField] private GameObject messageCanvas;
    [SerializeField] private TextMeshProUGUI messageText;

    [Header("Message Content")]
    [TextArea(2, 3)]
    [SerializeField] private string gateMessage = "Key entered, way to SouthGardens!";
    [SerializeField] private float messageDuration = 3f;

    [Header("Black Screen")]
    [SerializeField] private float blackScreenDelay = 0.5f;
    [SerializeField] private float darknessWaitTime = 2f;

    [Header("Teleport")]
    [SerializeField] private Transform spawnPoint;

    [Header("References")]
    [SerializeField] private CutsceneChief cutsceneChief;

    private Transform player;
    private PlayerController playerController;
    private Animator playerAnimator;
    private bool isPlayerNear;
    private bool hasBeenActivated;
    private GameObject blackScreen;

    private void Start()
    {
        Debug.Log($"[ActGateSimple] Initialized at {transform.position}");

        if (cutsceneChief == null)
        {
            cutsceneChief = FindObjectOfType<CutsceneChief>();
        }

        CreateBlackScreen();

        if (messageCanvas != null)
        {
            messageCanvas.SetActive(false);
        }

        if (spawnPoint == null)
        {
            Debug.LogWarning("[ActGateSimple] No spawn point assigned!");
        }
    }

    private void Update()
    {
        if (hasBeenActivated) return;

        CheckPlayerProximity();

        if (isPlayerNear)
        {
            TriggerGate();
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
                playerAnimator = player.GetComponent<Animator>();
            }
            else
            {
                return;
            }
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool wasNear = isPlayerNear;
        isPlayerNear = distance <= activationRadius;
    }

    private void TriggerGate()
    {
        if (hasBeenActivated) return;

        hasBeenActivated = true;

        Debug.Log($"[ActGateSimple] 🚪 Gate triggered! Starting sequence...");

        StartCoroutine(GateSequence());
    }

    private IEnumerator GateSequence()
    {
        Debug.Log("[ActGateSimple] ========== SEQUENCE START ==========");

        // 1. Player'ı dondur ve idle yap
        if (playerController != null)
        {
            playerController.FreezePlayer();
            Debug.Log("[ActGateSimple] ✅ Player frozen");
        }

        if (playerAnimator != null)
        {
            // Velocity'yi sıfırla ki idle animasyona geçsin
            playerAnimator.SetFloat("Speed", 0f);
            playerAnimator.SetBool("isRunning", false);
            Debug.Log("[ActGateSimple] ✅ Player set to idle animation");
        }

        // 2. Mesajı göster (3 saniye)
        ShowMessage(gateMessage);
        Debug.Log($"[ActGateSimple] 💬 Showing message for {messageDuration}s...");
        yield return new WaitForSeconds(messageDuration);
        HideMessage();
        Debug.Log("[ActGateSimple] ✅ Message hidden");

        // 3. Ekranı karart
        Debug.Log("[ActGateSimple] ⚫ Black screen ON");
        ShowBlackScreen();

        Debug.Log($"[ActGateSimple] ⏱️ Waiting {darknessWaitTime}s in darkness...");
        yield return new WaitForSeconds(darknessWaitTime);

        // 4. Player'ı teleport et
        if (player != null && spawnPoint != null)
        {
            Debug.Log($"[ActGateSimple] 📍 Teleporting player to: {spawnPoint.position}");
            player.position = spawnPoint.position;
        }

        // 5. State ilerlet
        if (cutsceneChief != null)
        {
            Debug.Log("[ActGateSimple] ⏩ Advancing to next state...");
            cutsceneChief.AdvanceState();
            Debug.Log("[ActGateSimple] ✅ State advanced");
        }

        // 6. Ekranı aç
        Debug.Log("[ActGateSimple] ⚪ Black screen OFF - BAM!");
        HideBlackScreen();

        // 7. Player'ı çöz
        if (playerController != null)
        {
            playerController.UnfreezePlayer();
            Debug.Log("[ActGateSimple] ✅ Player unfrozen");
        }

        Debug.Log("[ActGateSimple] ========== SEQUENCE END ==========");
    }

    #region Message System

    private void ShowMessage(string text)
    {
        if (messageCanvas != null)
        {
            messageCanvas.SetActive(true);
        }

        if (messageText != null)
        {
            messageText.text = text;
        }

        Debug.Log($"[ActGateSimple] 💬 Message: {text}");
    }

    private void HideMessage()
    {
        if (messageCanvas != null)
        {
            messageCanvas.SetActive(false);
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