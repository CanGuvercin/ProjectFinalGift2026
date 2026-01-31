using System.Collections;
using UnityEngine;

public class ZeilBossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 250f;
    private float currentHealth;
    
    [Header("Bullet Patterns")]
    [SerializeField] private GameObject normalBulletPrefab;
    [SerializeField] private GameObject megaBulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float normalBulletSpeed = 5f;
    [SerializeField] private float spiralInterval = 0.2f;
    [SerializeField] private int spiralWaveCount = 6;
    [SerializeField] private int bulletsPerWave = 5; // 72° için 5 bullet
    
    [Header("MegaBullet Settings")]
    [SerializeField] private float megaBulletInterval = 3f;
    [SerializeField] private int maxActiveMegaBullets = 2;
    private int currentMegaBullets = 0;
    
    [Header("Ball Charge")]
    [SerializeField] private float chargeSpeed = 20f;
    [SerializeField] private int chargesPerCycle = 2;
    [SerializeField] private int chargeDamage = 2; // int olmalı
    [SerializeField] private float telegraphDuration = 0.3f;
    [SerializeField] private float chargeDuration = 0.1f;
    private int currentChargeCount = 0;
    private Vector2 chargeTarget;
    
    [Header("Colliders")]
    [SerializeField] private GameObject slimeCollider;
    [SerializeField] private GameObject ballCollider;
    
    [Header("Audio")]
    [SerializeField] private AudioClip crawlSound;
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip rollSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] private AudioClip crushSound;
    [SerializeField] private AudioSource audioSource;
    
    [Header("Attack Pattern")]
    [SerializeField] private int slimeAttacksBeforeBall = 4; // 3-4 dalga mermi
    private int currentSlimeAttacks = 0;
    
    private enum BossState
    {
        SlimeIdle,
        SlimeShooting,
        BallTransition,
        BallStand,
        BallCharging,
        Death
    }
    
    private BossState currentState;
    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        currentState = BossState.SlimeIdle;
        
        // Player referansı yoksa bul
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        // Başlangıçta slime collider aktif
        SetColliderMode(true);
        
        // Boss pattern başlat
        StartCoroutine(BossAI());
    }

    void Update()
    {
        // Death check
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
    }

    #region Boss AI Pattern
    
    IEnumerator BossAI()
    {
        yield return new WaitForSeconds(1f); // Başlangıç bekleme
        
        while (!isDead)
        {
            // Slime Mode Attacks
            currentSlimeAttacks = 0;
            while (currentSlimeAttacks < slimeAttacksBeforeBall)
            {
                yield return StartCoroutine(SlimeAttackSequence());
                currentSlimeAttacks++;
                yield return new WaitForSeconds(1f); // Saldırılar arası bekleme
            }
            
            // Ball Mode Attacks
            yield return StartCoroutine(BallAttackSequence());
            
            yield return new WaitForSeconds(1.5f); // Cycle arası bekleme
        }
    }
    
    IEnumerator SlimeAttackSequence()
    {
        // Random: Spiral ya da MegaBullet
        bool useSpiral = Random.value > 0.3f; // %70 spiral, %30 mega
        
        if (useSpiral)
        {
            yield return StartCoroutine(SpiralBulletAttack());
        }
        else
        {
            yield return StartCoroutine(MegaBulletAttack());
        }
    }
    
    IEnumerator SpiralBulletAttack()
    {
        currentState = BossState.SlimeShooting;
        animator.SetBool("isShooting", true);
        
        PlaySound(shootSound);
        
        // Spiral waves
        for (int wave = 0; wave < spiralWaveCount; wave++)
        {
            float angleOffset = wave * 15f; // Her dalga 15° dönecek
            
            // 5 bullet, 72° arayla
            for (int i = 0; i < bulletsPerWave; i++)
            {
                float angle = (i * 72f) + angleOffset;
                FireNormalBullet(angle);
            }
            
            yield return new WaitForSeconds(spiralInterval);
        }
        
        animator.SetBool("isShooting", false);
        currentState = BossState.SlimeIdle;
        
        yield return new WaitForSeconds(0.5f);
    }
    
    IEnumerator MegaBulletAttack()
    {
        // Max 2 megabullet kontrolü
        if (currentMegaBullets >= maxActiveMegaBullets)
        {
            yield break;
        }
        
        currentState = BossState.SlimeShooting;
        animator.SetBool("isShooting", true);
        
        PlaySound(shootSound);
        
        // İlk megabullet
        FireMegaBullet();
        
        yield return new WaitForSeconds(megaBulletInterval);
        
        // İkinci megabullet
        if (currentMegaBullets < maxActiveMegaBullets)
        {
            FireMegaBullet();
        }
        
        animator.SetBool("isShooting", false);
        currentState = BossState.SlimeIdle;
        
        yield return new WaitForSeconds(0.5f);
    }
    
    IEnumerator BallAttackSequence()
    {
        // Ball form'a geçiş
        currentState = BossState.BallTransition;
        animator.SetTrigger("ToTopForm");
        PlaySound(rollSound);
        
        yield return new WaitForSeconds(0.8f); // BallUp animasyon süresi
        
        currentState = BossState.BallStand;
        SetColliderMode(false); // Ball collider aktif
        
        // Charge saldırıları
        currentChargeCount = 0;
        while (currentChargeCount < chargesPerCycle)
        {
            yield return StartCoroutine(ChargeAttack());
            currentChargeCount++;
            yield return new WaitForSeconds(0.8f); // Charge'lar arası bekleme
        }
        
        // Slime form'a dönüş
        animator.SetTrigger("BackToSlime");
        yield return new WaitForSeconds(0.8f); // BallDown animasyon süresi
        
        SetColliderMode(true); // Slime collider aktif
        currentState = BossState.SlimeIdle;
    }
    
    IEnumerator ChargeAttack()
    {
        // Player pozisyonunu kaydet
        chargeTarget = player.position;
        
        // Telegraph (kırmızılaşma)
        currentState = BossState.BallCharging;
        animator.SetTrigger("Charge");
        
        yield return new WaitForSeconds(telegraphDuration); // 0.3s uyarı
        
        // CHARGE! (ultra hızlı돌진)
        Vector2 direction = (chargeTarget - (Vector2)transform.position).normalized;
        rb.velocity = direction * chargeSpeed;
        
        PlaySound(crushSound);
        
        yield return new WaitForSeconds(chargeDuration); // 0.1s돌진
        
        // Dur
        rb.velocity = Vector2.zero;
        
        // StandBall'a dön (Has Exit Time ile otomatik dönecek)
        currentState = BossState.BallStand;
        
        yield return new WaitForSeconds(0.3f); // Kısa dinlenme
    }
    
    #endregion
    
    #region Bullet Firing
    
    void FireNormalBullet(float angle)
    {
        GameObject bullet = Instantiate(normalBulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        
        // Açıya göre yön hesapla
        float radians = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        
        // Bullet'a hız ver (Rigidbody2D veya custom hareket)
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * normalBulletSpeed;
        }
    }
    
    void FireMegaBullet()
    {
        GameObject megaBullet = Instantiate(megaBulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        
        // MegaBullet script'ine player referansı ver
        MegaBullet megaScript = megaBullet.GetComponent<MegaBullet>();
        if (megaScript != null)
        {
            megaScript.Initialize(player);
            currentMegaBullets++;
        }
    }
    
    // MegaBullet destroy olduğunda call edilecek
    public void OnMegaBulletDestroyed()
    {
        currentMegaBullets--;
    }
    
    #endregion
    
    #region Damage & Death
    
    public void TakeDamage(float damage)
    {
        if (isDead) return;
        
        // Sadece Slime modunda hasar al
        if (currentState == BossState.SlimeIdle || currentState == BossState.SlimeShooting)
        {
            currentHealth -= damage;
            PlaySound(hurtSound);
            
            // Hit flash effect eklenebilir
        }
    }
    
    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        
        animator.SetTrigger("isDeath");
        
        rb.velocity = Vector2.zero;
        
        // Collider'ları kapat
        slimeCollider.SetActive(false);
        ballCollider.SetActive(false);
        
        // Death sonrası logic (cutscene trigger, etc.)
        StartCoroutine(HandleDeath());
    }
    
    IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(2f); // Death animasyon süresi
        
        // Burada cutscene trigger, game state değişikliği vs. yapabilirsin
        Debug.Log("Boss defeated!");
        
        // Destroy veya deaktif et
        // Destroy(gameObject);
    }
    
    #endregion
    
    #region Collider Management
    
    void SetColliderMode(bool isSlimeMode)
    {
        slimeCollider.SetActive(isSlimeMode);
        ballCollider.SetActive(!isSlimeMode);
    }
    
    #endregion
    
    #region Audio
    
    void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }
    
    #endregion
    
    #region Collision Detection
    
    void OnTriggerEnter2D(Collider2D other)
    {
        // Ball modunda player'a temas
        if (currentState == BossState.BallCharging && other.CompareTag("Player"))
        {
            // Player'a hasar ver - PlayerController.TakeDamage(int damage, Vector2 damageSourcePos)
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(chargeDamage, transform.position);
                PlaySound(crushSound);
            }
        }
    }
    
    #endregion
}