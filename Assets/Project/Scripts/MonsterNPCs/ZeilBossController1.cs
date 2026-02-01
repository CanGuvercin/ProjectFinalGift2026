using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ZeilBossController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private Transform player;
    
    [Header("Health")]
    [SerializeField] private float maxHealth = 250f;
    private float currentHealth;
    
    [Header("Boss Health Bar UI")]
    [SerializeField] private GameObject bossHealthBarRoot; // BossFightUI GameObject
    [SerializeField] private Image healthBarBG; // Arka plan (koyu)
    [SerializeField] private Image healthBarFill; // Kırmızı bar
    [SerializeField] private float healthBarFadeInDuration = 2f; // Fade in süresi
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Bullet Patterns")]
    [SerializeField] private GameObject normalBulletPrefab;
    [SerializeField] private GameObject megaBulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float normalBulletSpeed = 5f;
    [SerializeField] private float spiralInterval = 0.15f;
    [SerializeField] private int spiralWaveCount = 6;
    [SerializeField] private int bulletsPerWave = 5;
    
    [Header("Slime Movement")]
    [SerializeField] private float slimeIdleMoveSpeed = 1f;
    [SerializeField] private float slimeShootingMoveSpeed = 0.5f;
    
    [Header("MegaBullet Settings")]
    [SerializeField] private float megaBulletInterval = 3f;
    [SerializeField] private int maxActiveMegaBullets = 2;
    private int currentMegaBullets = 0;
    
    [Header("Ball Charge")]
    [SerializeField] private float chargeSpeed = 25f;
    [SerializeField] private float chargeDistance = 8f;
    [SerializeField] private int chargesPerCycle = 2;
    [SerializeField] private int chargeDamage = 2;
    [SerializeField] private float telegraphDuration = 0.3f;
    private int currentChargeCount = 0;
    private Vector2 chargeTarget;
    private bool chargeHitWall = false;
    
    [Header("Animation Timings")]
    [SerializeField] private float ballUpDuration = 1.2f;
    [SerializeField] private float ballDownDuration = 0.5f;
    
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
    [SerializeField] private int slimeAttacksBeforeBall = 4;
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
    
    private float targetHealthFillAmount = 1f;
    private CanvasGroup healthBarCanvasGroup;

    void Start()
    {
        currentHealth = maxHealth;
        currentState = BossState.SlimeIdle;
        
        if (player == null)
        {
            player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }
        
        SetColliderMode(true);
        
        // Health bar başlangıç ayarı
        InitializeHealthBar();
        
        StartCoroutine(BossAI());
        StartCoroutine(SlimeIdleMovement());
    }

    void Update()
    {
        if (currentHealth <= 0 && !isDead)
        {
            Die();
        }
        
        // Smooth health bar update
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(
                healthBarFill.fillAmount, 
                targetHealthFillAmount, 
                Time.deltaTime * 8f
            );
        }
    }

    #region Health Bar Management
    
    void InitializeHealthBar()
    {
        if (bossHealthBarRoot == null)
        {
            Debug.LogWarning("[Boss] Health bar root not assigned!");
            return;
        }
        
        // CanvasGroup ekle veya bul (fade için)
        healthBarCanvasGroup = bossHealthBarRoot.GetComponent<CanvasGroup>();
        if (healthBarCanvasGroup == null)
        {
            healthBarCanvasGroup = bossHealthBarRoot.AddComponent<CanvasGroup>();
        }
        
        // Başlangıçta invisible
        healthBarCanvasGroup.alpha = 0f;
        bossHealthBarRoot.SetActive(true);
        
        // Health bar'ı full yap
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
        }
        targetHealthFillAmount = 1f;
        
        // Fade in başlat
        StartCoroutine(FadeInHealthBar());
    }
    
    IEnumerator FadeInHealthBar()
    {
        float elapsed = 0f;
        
        while (elapsed < healthBarFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / healthBarFadeInDuration;
            float curveT = fadeInCurve.Evaluate(t);
            
            if (healthBarCanvasGroup != null)
            {
                healthBarCanvasGroup.alpha = curveT;
            }
            
            yield return null;
        }
        
        if (healthBarCanvasGroup != null)
        {
            healthBarCanvasGroup.alpha = 1f;
        }
    }
    
    void UpdateHealthBar()
    {
        targetHealthFillAmount = Mathf.Clamp01(currentHealth / maxHealth);
    }
    
    void HideHealthBar()
    {
        if (bossHealthBarRoot != null)
        {
            StartCoroutine(FadeOutHealthBar());
        }
    }
    
    IEnumerator FadeOutHealthBar()
    {
        float elapsed = 0f;
        float duration = 1f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = 1f - (elapsed / duration);
            
            if (healthBarCanvasGroup != null)
            {
                healthBarCanvasGroup.alpha = t;
            }
            
            yield return null;
        }
        
        if (bossHealthBarRoot != null)
        {
            bossHealthBarRoot.SetActive(false);
        }
    }
    
    #endregion

    #region Boss AI Pattern
    
    IEnumerator BossAI()
    {
        yield return new WaitForSeconds(1f);
        
        while (!isDead)
        {
            currentSlimeAttacks = 0;
            while (currentSlimeAttacks < slimeAttacksBeforeBall)
            {
                yield return StartCoroutine(SlimeAttackSequence());
                currentSlimeAttacks++;
                yield return new WaitForSeconds(1f);
            }
            
            yield return StartCoroutine(BallAttackSequence());
            
            yield return new WaitForSeconds(1.5f);
        }
    }
    
    IEnumerator SlimeIdleMovement()
    {
        while (!isDead)
        {
            if (player != null && (currentState == BossState.SlimeIdle || currentState == BossState.SlimeShooting))
            {
                float moveSpeed = currentState == BossState.SlimeShooting ? slimeShootingMoveSpeed : slimeIdleMoveSpeed;
                
                Vector2 direction = (player.position - transform.position).normalized;
                transform.position = Vector2.MoveTowards(transform.position, player.position, moveSpeed * Time.deltaTime);
            }
            
            yield return null;
        }
    }
    
    IEnumerator SlimeAttackSequence()
    {
        bool useSpiral = Random.value > 0.3f;
        
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
        
        bool clockwise = Random.value > 0.5f;
        float rotationDirection = clockwise ? 1f : -1f;
        
        for (int wave = 0; wave < spiralWaveCount; wave++)
        {
            float angleOffset = wave * 15f * rotationDirection;
            
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
        if (currentMegaBullets >= maxActiveMegaBullets)
        {
            yield break;
        }
        
        currentState = BossState.SlimeShooting;
        animator.SetBool("isShooting", true);
        
        PlaySound(shootSound);
        
        FireMegaBullet();
        
        yield return new WaitForSeconds(megaBulletInterval);
        
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
        currentState = BossState.BallTransition;
        animator.SetTrigger("ToTopForm");
        PlaySound(rollSound);
        
        yield return new WaitForSeconds(ballUpDuration);
        
        currentState = BossState.BallStand;
        SetColliderMode(false);
        
        currentChargeCount = 0;
        while (currentChargeCount < chargesPerCycle)
        {
            yield return StartCoroutine(ChargeAttack());
            currentChargeCount++;
            
            if (currentChargeCount < chargesPerCycle)
            {
                yield return new WaitForSeconds(1f);
            }
        }
        
        animator.SetTrigger("BackToSlime");
        
        yield return new WaitForSeconds(ballDownDuration);
        
        SetColliderMode(true);
        currentState = BossState.SlimeIdle;
    }
    
    IEnumerator ChargeAttack()
    {
        if (player == null) yield break;
        
        chargeHitWall = false;
        
        Vector2 direction = (player.position - transform.position).normalized;
        chargeTarget = (Vector2)player.position + (direction * chargeDistance);
        
        currentState = BossState.BallCharging;
        animator.SetTrigger("Charge");
        
        yield return new WaitForSeconds(telegraphDuration);
        
        rb.velocity = direction * chargeSpeed;
        
        PlaySound(crushSound);
        
        float maxChargeTime = chargeDistance / chargeSpeed;
        float elapsed = 0f;
        
        while (elapsed < maxChargeTime && !chargeHitWall)
        {
            if (rb.velocity.magnitude < chargeSpeed * 0.2f)
            {
                break;
            }
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        rb.velocity = Vector2.zero;
        
        currentState = BossState.BallStand;
        
        yield return new WaitForSeconds(0.3f);
    }
    
    #endregion
    
    #region Bullet Firing
    
    void FireNormalBullet(float angle)
    {
        GameObject bullet = Instantiate(normalBulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        
        float radians = angle * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        
        Rigidbody2D bulletRb = bullet.GetComponent<Rigidbody2D>();
        if (bulletRb != null)
        {
            bulletRb.velocity = direction * normalBulletSpeed;
        }
    }
    
    void FireMegaBullet()
    {
        GameObject megaBullet = Instantiate(megaBulletPrefab, bulletSpawnPoint.position, Quaternion.identity);
        
        MegaBullet megaScript = megaBullet.GetComponent<MegaBullet>();
        if (megaScript != null)
        {
            megaScript.Initialize(player);
            currentMegaBullets++;
        }
    }
    
    public void OnMegaBulletDestroyed()
    {
        currentMegaBullets--;
        if (currentMegaBullets < 0) currentMegaBullets = 0;
    }
    
    #endregion
    
    #region Damage & Death
    
   public void TakeDamage(float damage)
{
    if (isDead) return;
    
    if (currentState == BossState.SlimeIdle || currentState == BossState.SlimeShooting)
    {
        currentHealth -= damage;
        Debug.Log($"[Boss] Took {damage} damage! Current HP: {currentHealth}/{maxHealth}");
        
        PlaySound(hurtSound);
        
        // Health bar güncelle
        UpdateHealthBar();
        Debug.Log($"[Boss] Health bar updated - Fill target: {targetHealthFillAmount}");
    }
    else
    {
        Debug.Log($"[Boss] Immune! Current state: {currentState}");
    }
}
    
    void Die()
    {
        isDead = true;
        StopAllCoroutines();
        
        animator.SetTrigger("isDeath");
        
        rb.velocity = Vector2.zero;
        
        slimeCollider.SetActive(false);
        ballCollider.SetActive(false);
        
        // Health bar'ı gizle
        HideHealthBar();
        
        StartCoroutine(HandleDeath());
    }
    
    IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(2f);
        
        Debug.Log("[Boss] Zeil defeated!");
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
    Debug.Log($"[Boss] OnTriggerEnter2D - Collider: {other.gameObject.name} | Tag: {other.tag}");
    
    // Ball mode collision with player
    if (currentState == BossState.BallCharging && other.CompareTag("Player"))
    {
        PlayerController playerController = other.GetComponent<PlayerController>();
        if (playerController != null)
        {
            playerController.TakeDamage(chargeDamage, transform.position);
            PlaySound(crushSound);
        }
    }
    
    // KILIÇ HASARI TEST
    if (other.CompareTag("PlayerAttack") || other.gameObject.layer == LayerMask.NameToLayer("PlayerAttack"))
    {
        Debug.Log("[Boss] SWORD HIT DETECTED!");
        TakeDamage(10); // Test için 10 hasar
    }
}
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (currentState == BossState.BallCharging)
        {
            if (collision.gameObject.CompareTag("Obstacle") || 
                collision.gameObject.layer == LayerMask.NameToLayer("Obstacle") ||
                collision.gameObject.CompareTag("Wall"))
            {
                chargeHitWall = true;
                rb.velocity = Vector2.zero;
            }
        }
    }
    
    #endregion
}