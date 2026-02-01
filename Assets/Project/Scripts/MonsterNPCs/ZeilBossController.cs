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
    [SerializeField] private GameObject bossHealthBarRoot;
    [SerializeField] private Image healthBarBG;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private float healthBarFadeInDuration = 2f;
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
    
    [Header("Ball Charge - Rolling Attack")]
    [SerializeField] private float chargeSpeed = 12f;
    [SerializeField] private float chargeMaxDuration = 3f;
    [SerializeField] private float chargeTelegraphDelay = 0.15f;
    [SerializeField] private float chargeRotationSpeed = 720f; // Derece/saniye
    [SerializeField] private float rotationResetSpeed = 5f; // Smooth reset hızı
    [SerializeField] private int chargesPerCycle = 2;
    [SerializeField] private int chargeDamage = 2;
    [SerializeField] private float telegraphDuration = 0.3f;
    private int currentChargeCount = 0;
    private bool chargeHitWall = false;
    private bool isRolling = false;
    
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
        
        // Rotation freeze
        rb.freezeRotation = true;
        
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
        
        if (healthBarFill != null && !isDead)
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
        
        healthBarCanvasGroup = bossHealthBarRoot.GetComponent<CanvasGroup>();
        if (healthBarCanvasGroup == null)
        {
            healthBarCanvasGroup = bossHealthBarRoot.AddComponent<CanvasGroup>();
        }
        
        healthBarCanvasGroup.alpha = 0f;
        bossHealthBarRoot.SetActive(true);
        
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = 1f;
        }
        targetHealthFillAmount = 1f;
        
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
        
        // Player'ın şu anki pozisyonu
        Vector2 targetPosition = player.position;
        
        // Telegraph
        currentState = BossState.BallCharging;
        animator.SetTrigger("Charge");
        
        yield return new WaitForSeconds(telegraphDuration);
        
        // 0.15s bekle
        yield return new WaitForSeconds(chargeTelegraphDelay);
        
        // Charge yönü
        Vector2 chargeDirection = (targetPosition - (Vector2)transform.position).normalized;
        
        // ROLLING BAŞLAT
        isRolling = true;
        rb.freezeRotation = false; // Rotation aktif
        
        // Rotation yönü (Y'ye göre)
        float rotationDir = chargeDirection.y > 0 ? -1f : 1f; // Y>0: Saat yönü, Y<0: Ters
        
        PlaySound(crushSound);
        
        // Charge başla
        rb.velocity = chargeDirection * chargeSpeed;
        
        float chargeElapsed = 0f;
        
        while (chargeElapsed < chargeMaxDuration && !chargeHitWall && isRolling)
        {
            // Dönme animasyonu
            float rotationAmount = chargeRotationSpeed * rotationDir * Time.deltaTime;
            rb.MoveRotation(rb.rotation + rotationAmount);
            
            chargeElapsed += Time.deltaTime;
            yield return null;
        }
        
        // Charge bitti - DUR VE ROTATION RESETLE
        isRolling = false;
        rb.velocity = Vector2.zero;
        
        // Smooth rotation reset (Z → 0)
        yield return StartCoroutine(ResetRotation());
        
        rb.freezeRotation = true; // Rotation freeze
        
        currentState = BossState.BallStand;
        
        yield return new WaitForSeconds(0.3f);
    }
    
    IEnumerator ResetRotation()
    {
        float currentRotation = transform.eulerAngles.z;
        
        // -180 ile 180 arası normalize et
        if (currentRotation > 180f)
        {
            currentRotation -= 360f;
        }
        
        float elapsed = 0f;
        float duration = Mathf.Abs(currentRotation) / (rotationResetSpeed * 90f); // Dinamik süre
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            
            float newRotation = Mathf.Lerp(currentRotation, 0f, t);
            transform.rotation = Quaternion.Euler(0f, 0f, newRotation);
            
            yield return null;
        }
        
        // Kesin 0'a çek
        transform.rotation = Quaternion.identity;
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
            
            PlaySound(hurtSound);
            UpdateHealthBar();
        }
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        currentState = BossState.Death;
        
        StopAllCoroutines();
        
        animator.ResetTrigger("isDeath");
        animator.SetTrigger("isDeath");
        
        rb.velocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Static;
        
        slimeCollider.SetActive(false);
        ballCollider.SetActive(false);
        
        HideHealthBar();
        
        StartCoroutine(HandleDeath());
    }
    
    IEnumerator HandleDeath()
    {
        yield return new WaitForSeconds(0.2f);
        
        float animDuration = 2f;
        
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        if (stateInfo.IsName("Death"))
        {
            animDuration = stateInfo.length / animator.speed;
        }
        
        yield return new WaitForSeconds(animDuration);
        
        Debug.Log("[Boss] Zeil defeated! Advancing state...");
        
        CutsceneChief chief = FindObjectOfType<CutsceneChief>();
        if (chief != null)
        {
            chief.AdvanceState();
        }
        
        Destroy(gameObject);
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
        // Ball charge - Player hasar
        if (currentState == BossState.BallCharging && other.CompareTag("Player"))
        {
            PlayerController playerController = other.GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerController.TakeDamage(chargeDamage, transform.position);
                PlaySound(crushSound);
            }
        }
        
        // Player kılıç hasarı
        if (other.CompareTag("PlayerAttack") || other.gameObject.name.Contains("HitBox"))
        {
            TakeDamage(10);
        }
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        // DUVARA ÇARPMA - CHARGE DURUR
        if (currentState == BossState.BallCharging && isRolling)
        {
            if (collision.gameObject.CompareTag("Obstacle") || 
                collision.gameObject.CompareTag("Wall") ||
                collision.gameObject.layer == LayerMask.NameToLayer("Obstacle"))
            {
                Debug.Log("[Boss] HIT WALL! Stopping charge...");
                
                chargeHitWall = true;
                isRolling = false;
                
                // ANİ DUR
                rb.velocity = Vector2.zero;
                
                // Rotation reset başlat (ChargeAttack coroutine halledecek)
            }
        }
    }
    
    #endregion
    
    public float GetCurrentHealth()
    {
        return currentHealth;
    }
    
    public float GetMaxHealth()
    {
        return maxHealth;
    }
}