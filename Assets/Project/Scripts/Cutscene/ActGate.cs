using UnityEngine;
using UnityEngine.InputSystem;
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
    private PlayerInputActions inputActions;

    private void Awake()
    {
        inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();
    }

    private void Start()
    {
        if (cutsceneChief == null)
            cutsceneChief = FindObjectOfType<CutsceneChief>();

        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        CreateBlackScreen();

        if (promptUI != null)
            promptUI.SetActive(false);

        if (dialogCanvas != null)
            dialogCanvas.SetActive(false);

        if (playableDirector != null)
            playableDirector.Stop();
    }

    private void Update()
    {
        if (hasBeenActivated) return;

        CheckPlayerProximity();

        if (useManualActivation)
        {
            if (isPlayerNear && inputActions.Player.Interact.WasPressedThisFrame())
                TriggerGate();
        }
        else
        {
            if (isPlayerNear)
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
            }
            else return;
        }

        float distance = Vector2.Distance(transform.position, player.position);
        bool wasNear = isPlayerNear;
        isPlayerNear = distance <= activationRadius;

        if (useManualActivation && isPlayerNear != wasNear && promptUI != null)
            promptUI.SetActive(isPlayerNear);
    }

    private void TriggerGate()
    {
        if (hasBeenActivated) return;

        hasBeenActivated = true;
        if (promptUI != null) promptUI.SetActive(false);

        StartCoroutine(GateSequence());
    }

    private IEnumerator GateSequence()
    {
        if (playerController != null)
            playerController.FreezePlayer();

        if (cutsceneChief != null)
        {
            cutsceneChief.DisableAutoAdvance();
            cutsceneChief.AdvanceState();
        }

        if (playableDirector != null)
        {
            playableDirector.Play();

            while (playableDirector.state == PlayState.Playing)
                yield return null;
        }

        ShowDialog(npcDialogue);

        if (audioSource != null && dialogSfx != null)
            audioSource.PlayOneShot(dialogSfx);

        yield return new WaitForSeconds(dialogueDuration);
        HideDialog();

        ShowBlackScreen();
        yield return new WaitForSeconds(2f);

        if (doorSound != null && audioSource != null)
            audioSource.PlayOneShot(doorSound);

        yield return new WaitForSeconds(doorSoundDuration + blackScreenDelay);

        if (player != null && spawnPoint != null)
            player.position = spawnPoint.position;

        if (cutsceneChief != null)
        {
            cutsceneChief.EnableAutoAdvance();
            cutsceneChief.AdvanceState();
        }

        HideBlackScreen();

        if (playerController != null)
            playerController.UnfreezePlayer();
    }

    private void ShowDialog(string text)
    {
        if (dialogCanvas != null)
            dialogCanvas.SetActive(true);
        if (dialogText != null)
            dialogText.text = text;
    }

    private void HideDialog()
    {
        if (dialogCanvas != null)
            dialogCanvas.SetActive(false);
    }

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
            blackScreen.SetActive(true);
    }

    private void HideBlackScreen()
    {
        if (blackScreen != null)
            blackScreen.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, activationRadius);

        if (spawnPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(spawnPoint.position, 0.5f);
            Gizmos.DrawLine(transform.position, spawnPoint.position);
        }
    }
}