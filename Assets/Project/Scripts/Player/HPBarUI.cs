using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("Heart Sprites")]
    [SerializeField] private Sprite heartFull;
    [SerializeField] private Sprite heartHalf;
    [SerializeField] private Sprite heartEmpty;
    
    [Header("Heart Images")]
    [SerializeField] private Image[] heartImages;
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    
    [Header("Settings")]
    [SerializeField] private int hpPerHeart = 20;
    
    private bool playerIsDead = false;
    
    private void Start()
    {
        if (player == null)
        {
            player = FindObjectOfType<PlayerController>();
        }
        
        UpdateHearts();
    }
    
    private void Update()
    {
        // Eğer player öldüyse HP bar'ı güncelleme (son durum kalsın)
        if (playerIsDead) return;
        
        // Player'ın HP'si 0 ise, artık güncelleme
        if (player != null && player.GetCurrentHealth() <= 0)
        {
            playerIsDead = true;
            return;
        }
        
        UpdateHearts();
    }
    
    private void UpdateHearts()
    {
        if (player == null || heartImages.Length == 0) return;
        
        int currentHP = player.GetCurrentHealth();
        int maxHP = player.GetMaxHealth();
        
        int totalHearts = Mathf.CeilToInt((float)maxHP / hpPerHeart);
        
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < totalHearts)
            {
                heartImages[i].enabled = true;
                
                int hpForThisHeart = currentHP - (i * hpPerHeart);
                
                if (hpForThisHeart >= hpPerHeart)
                {
                    heartImages[i].sprite = heartFull;
                }
                else if (hpForThisHeart > 0)
                {
                    heartImages[i].sprite = heartHalf;
                }
                else
                {
                    heartImages[i].sprite = heartEmpty;
                }
            }
            else
            {
                heartImages[i].enabled = false;
            }
        }
    }
}