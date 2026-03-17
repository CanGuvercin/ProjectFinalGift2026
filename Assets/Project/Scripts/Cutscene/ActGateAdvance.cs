using UnityEngine;
using UnityEngine.InputSystem;

public class ActGateAdvance : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 1.5f;
    [SerializeField] private bool autoActivate = false;

    [Header("UI")]
    [SerializeField] private GameObject pressELabel;

    [Header("References")]
    [SerializeField] private CutsceneChief cutsceneChief;

    private Transform player;
    private bool isPlayerNear = false;
    private bool hasBeenActivated = false;
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

        if (pressELabel != null)
            pressELabel.SetActive(false);
    }

    private void Update()
    {
        if (hasBeenActivated) return;

        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;
            player = playerObj.transform;
        }

        bool wasNear = isPlayerNear;
        isPlayerNear = Vector2.Distance(transform.position, player.position) <= activationRadius;

        if (isPlayerNear != wasNear && pressELabel != null)
            pressELabel.SetActive(isPlayerNear);

        if (isPlayerNear)
        {
            if (autoActivate || inputActions.Player.Interact.WasPressedThisFrame())
                Activate();
        }
    }

    private void Activate()
    {
        if (hasBeenActivated) return;
        hasBeenActivated = true;

        if (pressELabel != null)
            pressELabel.SetActive(false);

        if (cutsceneChief != null)
            cutsceneChief.AdvanceState();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}