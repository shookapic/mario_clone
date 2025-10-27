using System.Collections;
using UnityEngine;

public class Boss : MonoBehaviour
{
    [Header("Health Settings")]
    public int maxHealth = 10;
    private int currentHealth;
    
    [Header("Patrol Settings")]
    public float patrolSpeed = 2f;
    public float patrolDistance = 5f;
    public Transform leftLimit;
    public Transform rightLimit;
    private bool movingRight = true;
    private Vector3 startPosition;
    
    [Header("Attack Settings")]
    public float attackRange = 3f;
    public float attackCooldown = 2f;
    public float attackDamage = 1f;
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float projectileSpeed = 5f;
    private float lastAttackTime;
    
    [Header("Jump Detection")]
    public LayerMask playerLayer = 1;
    public float jumpDamageMultiplier = 1f;
    public float jumpDetectionHeight = 0.5f;
    
    [Header("Visual Feedback")]
    public Color hitColor = Color.red;
    public float hitFlashDuration = 0.2f;
    private Color originalColor;
    
    [Header("Victory Settings")]
    public bool triggerVictoryOnDeath = true;
    
    // Components
    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private MyPlayerMovement player;
    
    // State tracking
    private bool isDead = false;
    private bool isHit = false;
    
    void Start()
    {
        currentHealth = maxHealth;
        startPosition = transform.position;
        
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
        
        player = FindObjectOfType<MyPlayerMovement>();
        
        // patrols limits setup
        if (leftLimit == null || rightLimit == null)
        {
            SetupDefaultPatrolLimits();
        }
        
        Collider2D bossCollider = GetComponent<Collider2D>();
        if (bossCollider != null && bossCollider.isTrigger)
        {
            Debug.LogWarning("Boss collider is set as trigger! This may cause automatic damage. Setting to non-trigger.");
            bossCollider.isTrigger = false;
        }
        

        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "Enemy";
            Debug.Log("Boss tag set to Enemy for fireball detection");
        }
        
    }
    
    void SetupDefaultPatrolLimits()
    {
        GameObject leftLimitObj = new GameObject("BossLeftLimit");
        GameObject rightLimitObj = new GameObject("BossRightLimit");
        
        leftLimitObj.transform.position = startPosition + Vector3.left * patrolDistance;
        rightLimitObj.transform.position = startPosition + Vector3.right * patrolDistance;
        
        leftLimit = leftLimitObj.transform;
        rightLimit = rightLimitObj.transform;
        
        leftLimitObj.transform.SetParent(transform.parent);
        rightLimitObj.transform.SetParent(transform.parent);
    }
    
    void Update()
    {
        if (isDead) return;
        
        Patrol();
        
        if (player != null && Vector2.Distance(transform.position, player.transform.position) <= attackRange)
        {
            if (Time.time >= lastAttackTime + attackCooldown)
            {
                Attack();
                lastAttackTime = Time.time;
            }
        }
        
        UpdateAnimator();
    }
    
    void Patrol()
    {
        if (leftLimit == null || rightLimit == null) return;
        
        if (movingRight)
        {
            rb.linearVelocity = new Vector2(patrolSpeed, rb.linearVelocity.y);
            
            if (transform.position.x >= rightLimit.position.x)
            {
                movingRight = false;
                Flip();
            }
        }
        else
        {
            rb.linearVelocity = new Vector2(-patrolSpeed, rb.linearVelocity.y);
            
            if (transform.position.x <= leftLimit.position.x)
            {
                movingRight = true;
                Flip();
            }
        }
    }
    
    void Flip()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !spriteRenderer.flipX;
        }
    }
    
    void Attack()
    {
        if (player == null) return;
        
        if (animator != null)
            animator.SetTrigger("Attack");
        
        ShootProjectileAtPlayer();
        
        Debug.Log("Boss attacked!");
    }
    
    void ShootProjectileAtPlayer()
    {
        if (projectilePrefab == null)
        {
            Debug.LogWarning("No projectile prefab assigned to Boss!");
            return;
        }
        
        Vector3 spawnPos = transform.position;
        if (firePoint != null)
        {
            spawnPos = firePoint.position;
        }
        else
        {
            // Default: spawn slightly in front of boss
            spawnPos += spriteRenderer.flipX ? Vector3.left * 0.5f : Vector3.right * 0.5f;
        }
        
        Vector3 direction = (player.transform.position - spawnPos).normalized;
        
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        Rigidbody2D projectileRb = projectile.GetComponent<Rigidbody2D>();
        if (projectileRb == null)
        {
            projectileRb = projectile.AddComponent<Rigidbody2D>();
            projectileRb.gravityScale = 0; // No gravity for boss projectiles
        }
        
        projectileRb.linearVelocity = direction * projectileSpeed;
        
        // Add boss projectile component
        BossProjectile bossProj = projectile.GetComponent<BossProjectile>();
        if (bossProj == null)
        {
            bossProj = projectile.AddComponent<BossProjectile>();
        }
        bossProj.damage = (int)attackDamage;
        
        // Set projectile tag if not set
        if (projectile.tag == "Untagged")
        {
            projectile.tag = "EnemyProjectile";
        }
        
    }
    
    void UpdateAnimator()
    {
        if (animator == null) return;
        
        // Set movement speed parameter
        float speed = Mathf.Abs(rb.linearVelocity.x);
        animator.SetFloat("Speed", speed);
        
        // Set health parameter
        animator.SetFloat("Health", (float)currentHealth / maxHealth);
    }
    
    public void TakeDamage(int amount, DamageSource source = DamageSource.Unknown)
    {
        if (isDead || isHit) return;
        
        currentHealth -= amount;
        
        StartCoroutine(HitFlash());
        
        // Play hit animation
        if (animator != null)
            animator.SetTrigger("Hit");
        
        string sourceText = source == DamageSource.Fireball ? "fireball" : 
                           source == DamageSource.Jump ? "jump attack" : "unknown";
        Debug.Log($"Boss took {amount} damage from {sourceText}! Health: {currentHealth}/{maxHealth}");
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    IEnumerator HitFlash()
    {
        isHit = true;
        
        if (spriteRenderer != null)
        {
            spriteRenderer.color = hitColor;
            yield return new WaitForSeconds(hitFlashDuration);
            spriteRenderer.color = originalColor;
        }
        
        isHit = false;
    }
    
    void Die()
    {
        if (isDead) return;
        
        isDead = true;
        rb.linearVelocity = Vector2.zero;
        
        Debug.Log("Boss defeated!");
        
        // Play death animation
        if (animator != null)
            animator.SetTrigger("Die");
        
        // Stop movement
        rb.isKinematic = true;
        
        // Trigger victory
        if (triggerVictoryOnDeath)
        {
            StartCoroutine(TriggerVictory());
        }
        else
        {
            Destroy(gameObject, 2f);
        }
    }
    
    IEnumerator TriggerVictory()
    {
        // Wait for death animation
        yield return new WaitForSeconds(1f);
        
        // Try to find and activate a victory flag
        FlagPole victoryFlag = FindVictoryFlag();
        if (victoryFlag != null)
        {
            victoryFlag.ManualActivateCheckpoint(); // This will trigger level completion
            Debug.Log("Victory flag activated!");
        }
        else
        {
            // Create a temporary victory screen
            ShowVictoryMessage();
        }
        
        // Destroy boss after victory
        yield return new WaitForSeconds(2f);
        Destroy(gameObject);
    }
    
    FlagPole FindVictoryFlag()
    {
        FlagPole[] flags = FindObjectsOfType<FlagPole>();
        foreach (FlagPole flag in flags)
        {
            if (flag.isLevelEndFlag)
            {
                return flag;
            }
        }
        return null;
    }
    
    void ShowVictoryMessage()
    {
        // Add a simple victory component to show victory message
        GameObject victoryObj = new GameObject("VictoryMessage");
        VictoryDisplay victory = victoryObj.AddComponent<VictoryDisplay>();
        victory.ShowVictory();
    }
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        Debug.Log($"Boss collision detected with: {collision.gameObject.name}, tag: {collision.gameObject.tag}");
        
        // Handle fireball damage
        if (collision.gameObject.CompareTag("Fireball") || collision.gameObject.name.Contains("Fireball"))
        {
            Debug.Log("FIREBALL HIT BOSS!");
            TakeDamage(1, DamageSource.Fireball);
            Destroy(collision.gameObject); // Destroy the fireball
            return;
        }
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("PLAYER COLLIDED WITH BOSS!");
            
            bool isJumpAttack = IsPlayerJumpingOnBoss(collision);
            
            if (isJumpAttack)
            {
                Debug.Log("JUMP ATTACK SUCCESSFUL!");
                TakeDamage((int)jumpDamageMultiplier, DamageSource.Jump);
                
                Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
                if (playerRb != null)
                {
                    playerRb.linearVelocity = new Vector2(playerRb.linearVelocity.x, 10f);
                }
            }
        }
    }
    
    private bool IsPlayerJumpingOnBoss(Collision2D collision)
    {
        // Get player rigidbody to check velocity
        Rigidbody2D playerRb = collision.gameObject.GetComponent<Rigidbody2D>();
        
        // SIMPLIFIED CONDITIONS - much more forgiving:
        
        float playerY = collision.transform.position.y;
        float bossY = transform.position.y;
        bool playerAbove = playerY > (bossY + 0.1f);
        
        bool playerMovingDown = playerRb != null && playerRb.linearVelocity.y < 0f;
        
        MyPlayerMovement playerScript = collision.gameObject.GetComponent<MyPlayerMovement>();
        bool playerCanAttack = playerScript != null && !playerScript.IsInvincible();
        
        bool isValidJump = playerAbove && playerCanAttack;
        
        Debug.Log($"JUMP DEBUG - Player Y: {playerY}, Boss Y: {bossY}, Above: {playerAbove}, MovingDown: {playerMovingDown}, CanAttack: {playerCanAttack}, ValidJump: {isValidJump}");
        Debug.Log($"Player velocity: {playerRb?.linearVelocity}");
        
        return isValidJump;
    }
    
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Fireball") || other.name.Contains("Fireball"))
        {
            Debug.Log("Fireball hit boss via trigger!");
            TakeDamage(1, DamageSource.Fireball);
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Player"))
        {
            Debug.LogWarning("Player entered boss trigger - but this should NOT cause damage!");
        }
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        // Draw patrol limits
        if (leftLimit != null && rightLimit != null)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(leftLimit.position, rightLimit.position);
            Gizmos.DrawWireSphere(leftLimit.position, 0.3f);
            Gizmos.DrawWireSphere(rightLimit.position, 0.3f);
        }
        
        // Draw attack range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
        
        // Draw jump detection area
        Gizmos.color = Color.yellow;
        Vector3 jumpArea = transform.position + Vector3.up * jumpDetectionHeight;
        Gizmos.DrawWireCube(jumpArea, new Vector3(1f, 0.2f, 1f));
        
        // Draw health bar
        Vector3 healthBarPos = transform.position + Vector3.up * 2f;
        float healthPercentage = (float)currentHealth / maxHealth;
        
        Gizmos.color = Color.red;
        Gizmos.DrawLine(healthBarPos - Vector3.right, healthBarPos + Vector3.right);
        
        Gizmos.color = Color.green;
        Gizmos.DrawLine(healthBarPos - Vector3.right, healthBarPos + Vector3.right * (2 * healthPercentage - 1));
    }
    
    // MANUAL TEST METHOD - can be called from inspector or debug
    [ContextMenu("Test Boss Damage")]
    public void TestBossDamage()
    {
        Debug.Log("Testing boss damage manually!");
        TakeDamage(1, DamageSource.Jump);
    }
}

// Enum for damage source tracking
public enum DamageSource
{
    Unknown,
    Fireball,
    Jump
}

public class BossProjectile : MonoBehaviour
{
    public int damage = 1;
    public float lifetime = 5f;
    
    void Start()
    {
        Destroy(gameObject, lifetime);
        
        // Ensure projectile has a collider
        if (GetComponent<Collider2D>() == null)
        {
            CircleCollider2D col = gameObject.AddComponent<CircleCollider2D>();
            col.radius = 0.2f;
            col.isTrigger = false; // FIXED: Use collision, not trigger for better physics
        }
        
        // Ensure correct tag is set
        if (gameObject.tag == "Untagged")
        {
            gameObject.tag = "EnemyProjectile";
        }
        
        Debug.Log($"BossProjectile created with tag: {gameObject.tag}");
    }
    
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Boss projectile hit player via collision!");
            Destroy(gameObject);
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            Debug.Log("Boss projectile hit ground!");
            Destroy(gameObject);
        }
    }
    
    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Boss projectile hit player via trigger!");
            Destroy(gameObject);
        }
        else if (other.CompareTag("Ground"))
        {
            Debug.Log("Boss projectile hit ground!");
            Destroy(gameObject);
        }
    }
}

// Simple victory display component
public class VictoryDisplay : MonoBehaviour
{
    private bool showVictory = false;
    private float victoryTimer = 0f;
    private float victoryDuration = 5f;
    
    public void ShowVictory()
    {
        showVictory = true;
        victoryTimer = victoryDuration;
    }
    
    void Update()
    {
        if (showVictory)
        {
            victoryTimer -= Time.deltaTime;
            if (victoryTimer <= 0)
            {
                showVictory = false;
                Destroy(gameObject);
            }
        }
    }
    
    void OnGUI()
    {
        if (!showVictory) return;
        
        GUIStyle victoryStyle = new GUIStyle(GUI.skin.label);
        victoryStyle.fontSize = 48;
        victoryStyle.fontStyle = FontStyle.Bold;
        victoryStyle.alignment = TextAnchor.MiddleCenter;
        victoryStyle.normal.textColor = Color.gold;
        
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 201, Screen.height / 2 - 81, 402, 162), "BOSS DEFEATED!", victoryStyle);
        
        GUI.color = Color.gold;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 80, 400, 160), "BOSS DEFEATED!", victoryStyle);
        
        GUIStyle subStyle = new GUIStyle(GUI.skin.label);
        subStyle.fontSize = 20;
        subStyle.alignment = TextAnchor.MiddleCenter;
        subStyle.normal.textColor = Color.white;
        
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 20, 300, 30), "Victory!", subStyle);
    }
}
