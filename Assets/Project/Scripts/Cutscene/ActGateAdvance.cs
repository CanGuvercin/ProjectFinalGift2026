using UnityEngine;

public class ActGateAdvance : MonoBehaviour
{
    [Header("Activation Settings")]
    [SerializeField] private float activationRadius = 1.5f;
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool autoActivate = false; // true: yaklaşınca otomatik, false: E tuşu

    [Header("UI")]
    [SerializeField] private GameObject pressELabel;

    [Header("References")]
    [SerializeField] private CutsceneChief cutsceneChief;

    private Transform player;
    private bool isPlayerNear = false;
    private bool hasBeenActivated = false;

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
            if (autoActivate || Input.GetKeyDown(interactionKey))
                Activate();
        }
    }

    private void Activate()
    {
        if (hasBeenActivated) return;
        hasBeenActivated = true;

        if (pressELabel != null)
            pressELabel.SetActive(false);

        Debug.Log("[ActGateAdvance] AdvanceState called");

        if (cutsceneChief != null)
            cutsceneChief.AdvanceState();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;//
        Gizmos.DrawWireSphere(transform.position, activationRadius);
    }
}