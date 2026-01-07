using UnityEngine;
using System.Collections;
using TMPro;

public class State2Briefing : MonoBehaviour
{
    [Header("Dialog Timing")]
    [SerializeField] private float startDelay = 1f; // Dede gelme süresi
    [SerializeField] private float dialogDuration = 2.5f; // Her satır süresi
    [SerializeField] private float endDelay = 1f; // Dede gitme süresi
    
    [Header("Dialog UI")]
    [SerializeField] private GameObject dialogCanvas; // DialogBox GameObject
    [SerializeField] private TextMeshProUGUI dialogText; // Text component
    
    [Header("Dialog SFX")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip dialogSfx;
    
    [Header("Dialog Lines")]
    [TextArea(2, 3)]
    [SerializeField] private string[] dialogLines =
    {
        "Ali, sen seçilmiş olansın!",
        "Git çeşmeyi bul.",
        "Altındaki dungeon'da bir anahtar var.",
        "O anahtar ile okulun kuzey kapısını açabilirsin!"
    };
    
    [Header("Next State")]
    [SerializeField] private int nextState = 3; // State 3'e geç
    
    private PlayerController playerController;
    
    private void Start()
    {
        // Player'ı bul
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
        {
            playerController = playerObj.GetComponent<PlayerController>();
        }
        
        // Dialog başta kapalı
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }
    }
    
    private void OnEnable()
    {
        // State 2 aktif olunca cutscene başlat
        StartCoroutine(PlayBriefing());
    }
    
    private IEnumerator PlayBriefing()
    {
        // Player kontrolünü kapat
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        // 1. DEDE GELİYOR (1 saniye)
        yield return new WaitForSeconds(startDelay);
        
        // 2. DIALOG GÖSTER (Her satır 2.5 saniye)
        foreach (string line in dialogLines)
        {
            ShowDialog(line);
            
            // SFX çal
            if (audioSource != null && dialogSfx != null)
            {
                audioSource.PlayOneShot(dialogSfx);
            }
            
            // 2.5 saniye bekle
            yield return new WaitForSeconds(dialogDuration);
        }
        
        // Dialog'u gizle
        HideDialog();
        
        // 3. DEDE GİDİYOR (1 saniye)
        yield return new WaitForSeconds(endDelay);
        
        // 4. STATE 3'E GEÇ
        TransitionToNextState();
    }
    
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
        
        Debug.Log($"[State2] Dialog: {text}");
    }
    
    private void HideDialog()
    {
        if (dialogCanvas != null)
        {
            dialogCanvas.SetActive(false);
        }
    }
    
    private void TransitionToNextState()
    {
        Debug.Log($"[State2] Transitioning to State {nextState}");
        
        // Player kontrolünü aç
        if (playerController != null)
        {
            playerController.enabled = true;
        }
        
        // State değiştir
        CutsceneChief cutsceneChief = FindObjectOfType<CutsceneChief>();
        if (cutsceneChief != null)
        {
            cutsceneChief.SetState(nextState);
        }
    }
}