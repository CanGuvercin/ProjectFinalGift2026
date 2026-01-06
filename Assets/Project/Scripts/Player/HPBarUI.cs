using UnityEngine;
using UnityEngine.UI;

public class HPBarUI : MonoBehaviour
{
    [Header("Heart Sprites")]
    [SerializeField] private Sprite heartFull;   // ❤️
    [SerializeField] private Sprite heartHalf;   // 💔
    [SerializeField] private Sprite heartEmpty;  // 🖤
    
    [Header("Heart Images")]
    [SerializeField] private Image[] heartImages; // 5 tane (Heart_1 to Heart_5)
    
    [Header("Player Reference")]
    [SerializeField] private PlayerController player;
    
    [Header("Settings")]
    [SerializeField] private int hpPerHeart = 20; // 1 kalp = 20 HP
    
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
        UpdateHearts();
    }
    
    private void UpdateHearts()
    {
        if (player == null || heartImages.Length == 0) return;
        
        int currentHP = player.GetCurrentHealth();
        int maxHP = player.GetMaxHealth();
        
        // Kaç kalp göstermemiz lazım?
        int totalHearts = Mathf.CeilToInt((float)maxHP / hpPerHeart);
        
        // Her kalbi güncelle
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (i < totalHearts)
            {
                // Bu kalp kullanımda
                heartImages[i].enabled = true;
                
                // Bu kalpte kaç HP olmalı?
                int hpForThisHeart = currentHP - (i * hpPerHeart);
                
                if (hpForThisHeart >= hpPerHeart)
                {
                    // Tam kalp ❤️
                    heartImages[i].sprite = heartFull;
                }
                else if (hpForThisHeart > 0)
                {
                    // Yarım kalp 💔
                    heartImages[i].sprite = heartHalf;
                }
                else
                {
                    // Boş kalp 🖤
                    heartImages[i].sprite = heartEmpty;
                }
            }
            else
            {
                // Bu kalp kullanımda değil (max HP düşükse)
                heartImages[i].enabled = false;
            }
        }

        
    }
}