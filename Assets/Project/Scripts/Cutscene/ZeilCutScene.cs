using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ZeilCutScene : MonoBehaviour
{
    [Header("Characters")]
    [SerializeField] private Transform ali;
    [SerializeField] private Transform zeil;
    [SerializeField] private SpriteRenderer aliRenderer;
    [SerializeField] private SpriteRenderer zeilRenderer;
    
    [Header("Positions")]
    [SerializeField] private Transform aliStartPos;
    [SerializeField] private Transform aliDialogPos;
    [SerializeField] private Transform aliExitPos;
    
    [Header("Dialog UI")]
    [SerializeField] private GameObject dialogPanel;
    [SerializeField] private Text dialogText;
    [SerializeField] private Text nameText;
    
    [Header("Screen Fade")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;
    
    [Header("Settings")]
    [SerializeField] private float walkSpeed = 2f;
    [SerializeField] private string nextSceneName = "OutsideMap";
    
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip doorSound;
    
    private bool cutsceneStarted = false;
    
    private void Start()
    {
        // Başlangıç setup
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
        }
        
        // Ali'yi başlangıç pozisyonuna koy
        if (ali != null && aliStartPos != null)
        {
            ali.position = aliStartPos.position;
        }
    }
    
    private void Update()
    {
        // Cutscene'i başlatmak için trigger (elle çağırabilirsiniz de)
        if (!cutsceneStarted && Input.GetKeyDown(KeyCode.C))
        {
            StartCutscene();
        }
    }
    
    public void StartCutscene()
    {
        if (cutsceneStarted) return;
        cutsceneStarted = true;
        
        // Oyuncu kontrolünü kapat
        DisablePlayerControl();
        
        StartCoroutine(CutsceneSequence());
    }
    
    private IEnumerator CutsceneSequence()
    {
        // 1. Ali merdiven girişinden ilerler
        yield return StartCoroutine(MoveCharacter(ali, aliDialogPos.position));
        
        // 2. Ali sola bakar (Zeil'e)
        FlipCharacter(aliRenderer, true); // true = sola bak
        
        yield return new WaitForSeconds(0.3f);
        
        // 3. Dialog: Ali - "Zeil! You!"
        yield return StartCoroutine(ShowDialog("Ali", "Zeil! You!"));
        
        // 4. Dialog: Zeil - "Bwahahah..."
        yield return StartCoroutine(ShowDialog("Zeil", "Bwahahah. So you already came here?"));
        
        yield return new WaitForSeconds(0.5f);
        
        // 5. Dialog: Zeil - "Find a way..."
        yield return StartCoroutine(ShowDialog("Zeil", "Find a way to reach me. I will be waiting you in my room. Bwahahaha!"));
        
        yield return new WaitForSeconds(0.5f);
        
        // 6. Dialog'u kapat
        if (dialogPanel != null)
            dialogPanel.SetActive(false);
        
        // 7. Ali sağa döner
        FlipCharacter(aliRenderer, false); // false = sağa bak
        
        yield return new WaitForSeconds(0.3f);
        
        // 8. Ali kapıya yürür
        yield return StartCoroutine(MoveCharacter(ali, aliExitPos.position));
        
        // 9. Ekran kararır
        yield return StartCoroutine(FadeOut());
        
        // 10. Kapı sesi
        if (audioSource != null && doorSound != null)
        {
            audioSource.PlayOneShot(doorSound);
            yield return new WaitForSeconds(0.5f);
        }
        
        // 11. Yeni sahneye geç
        SceneManager.LoadScene(nextSceneName);
    }
    
    private IEnumerator MoveCharacter(Transform character, Vector3 targetPos)
    {
        if (character == null) yield break;
        
        Vector3 startPos = character.position;
        float distance = Vector3.Distance(startPos, targetPos);
        float duration = distance / walkSpeed;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            character.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        
        character.position = targetPos;
    }
    
    private IEnumerator ShowDialog(string characterName, string message)
    {
        if (dialogPanel != null)
            dialogPanel.SetActive(true);
        
        if (nameText != null)
            nameText.text = characterName;
        
        if (dialogText != null)
            dialogText.text = message;
        
        // Space veya Enter bekle
        while (!Input.GetKeyDown(KeyCode.Space) && !Input.GetKeyDown(KeyCode.Return))
        {
            yield return null;
        }
        
        yield return new WaitForSeconds(0.1f); // Input buffer
    }
    
    private IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        
        float elapsed = 0f;
        Color c = fadeImage.color;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, elapsed / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        
        c.a = 1f;
        fadeImage.color = c;
    }
    
    private void FlipCharacter(SpriteRenderer renderer, bool faceLeft)
    {
        if (renderer == null) return;
        renderer.flipX = faceLeft;
    }
    
    private void DisablePlayerControl()
    {
        // Player controller'ı devre dışı bırak
        var playerController = ali.GetComponent<PlayerController>();
        if (playerController != null)
            playerController.enabled = false;
        
        // Rigidbody'yi durdur
        var rb = ali.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.velocity = Vector2.zero;
    }
} //..