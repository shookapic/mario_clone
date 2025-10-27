using UnityEngine;

public class MyPlayerMovement : MonoBehaviour
{
    [Header("Fireball Settings")]
    public GameObject fireballPrefab;
    public Transform firePoint;
    public bool hasFirePower = false;
    public float fireballCooldown = 0.5f;
    private float fireballTimer = 0f;

    public float fireballSpeed = 8f;
    public float fireballArc = 0.5f;

    [Header("Size Settings")]
    public float growthScale = 1.2f; // 20% growth
    public bool isGrown = false;
    private Vector3 originalScale;
    private Vector2 originalColliderSize;
    private Vector2 originalColliderOffset;
    private Vector3 originalPosition;

    [Header("Debug Settings")]
    [Tooltip("Show hitbox visualization")]
    public bool showHitboxes = true;
    [Tooltip("Show ground detection raycast")]
    public bool showGroundRaycast = true;

    private short coins_collected = 0;
    public short Lives = 3;
    public float Speed = 5f;
    public float jumpForce = 10f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer spriteRenderer;
    private Collider2D playerCollider;

    private bool touchingGround;

    public Sprite coinSprite;
    public Sprite live;
    private bool showUI = true;

    private AudioSource jumpSound;

    private bool isInvincible = false;
    private float invincibilityDuration = 2.5f;
    private float invincibilityTimer = 0f;
    private float blinkSpeed = 0.1f;
    private float blinkTimer = 0f;
    private bool isVisible = true;

    private float levelTimer = 180f;

    // FPS counter variables, FPS system made with AI
    private float fps = 0f;
    private float fpsUpdateInterval = 0.5f;
    private float fpsAccumulator = 0f;
    private int fpsFrames = 0;
    private float fpsTimeLeft = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCollider = GetComponent<Collider2D>();
        jumpSound = GetComponent<AudioSource>();

        // Store original size values
        originalScale = transform.localScale;

        if (playerCollider != null)
        {
            if (playerCollider is BoxCollider2D boxCollider)
            {
                originalColliderSize = boxCollider.size;
                originalColliderOffset = boxCollider.offset;
            }
            else if (playerCollider is CircleCollider2D circleCollider)
            {
                originalColliderSize = new Vector2(circleCollider.radius * 2, circleCollider.radius * 2);
                originalColliderOffset = circleCollider.offset;
            }
        }

        if (rb == null) Debug.LogError("Rigidbody2D component not found on " + gameObject.name);
        if (anim == null) Debug.LogError("Animator component not found on " + gameObject.name);
        if (spriteRenderer == null) Debug.LogError("SpriteRenderer component not found on " + gameObject.name);
        if (playerCollider == null) Debug.LogError("Collider2D component not found on " + gameObject.name);
        if (jumpSound == null) Debug.LogWarning("AudioSource component not found on " + gameObject.name + ". Jump sound will not play.");

        // Warning if coin sprite is not assigned
        if (coinSprite == null) Debug.LogWarning("Coin sprite not assigned! Drag your coin sprite to the 'Coin Sprite' field in the inspector.");
        if (live == null) Debug.LogWarning("Live sprite not assigned! Drag your live sprite to the 'Live' field in the inspector.");

        // Initialize FPS counter
        fpsTimeLeft = fpsUpdateInterval;

        // Debug info about sprite pivot
        if (showHitboxes)
        {
            Debug.Log($"Sprite pivot: {spriteRenderer.sprite?.pivot}");
            Debug.Log($"Original transform position: {transform.position}");
            Debug.Log($"Original collider bounds: {playerCollider.bounds}");
        }

        // Load game state after initialization but handle positioning separately
        StartCoroutine(LoadGameStateAfterStart());
    }

    // Enhanced coroutine to handle state loading and positioning
    private System.Collections.IEnumerator LoadGameStateAfterStart()
    {
        yield return new WaitForEndOfFrame();
        
        // First check if there's a specific spawn point for this scene
        Vector3? spawnPosition = FindSpawnPosition();
        
        if (GameManager.Instance != null)
        {
            // Load game state (coins, lives, power-ups, etc.)
            GameManager.Instance.LoadGameState(this);
            Debug.Log("Player state loaded from GameManager on scene start");
            
            // Handle positioning separately
            if (spawnPosition.HasValue)
            {
                // Use scene-specific spawn point
                transform.position = spawnPosition.Value;
                Debug.Log($"Player positioned at scene spawn point: {spawnPosition.Value}");
            }
            else if (GameManager.Instance.GetPlayerPosition() != Vector3.zero)
            {
                // Use saved position from GameManager (for scene transitions without specific spawn)
                transform.position = GameManager.Instance.GetPlayerPosition();
                Debug.Log($"Player positioned at saved position: {GameManager.Instance.GetPlayerPosition()}");
            }
            
            // Reset velocity after positioning
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
        else if (spawnPosition.HasValue)
        {
            // Fallback: use spawn point even without GameManager
            transform.position = spawnPosition.Value;
            Debug.Log($"Player positioned at scene spawn point (no GameManager): {spawnPosition.Value}");
        }
    }

    // Find spawn position in current scene
    private Vector3? FindSpawnPosition()
    {
        // Look for various spawn point objects/components
        
        // Option 1: Look for SpawnPoint component
        SpawnPoint spawnPoint = FindObjectOfType<SpawnPoint>();
        if (spawnPoint != null)
        {
            return spawnPoint.GetSpawnPosition();
        }
        
        // Option 2: Look for GameObject with "PlayerSpawn" tag
        GameObject spawnObject = GameObject.FindWithTag("PlayerSpawn");
        if (spawnObject != null)
        {
            return spawnObject.transform.position;
        }
        
        // Option 3: Look for GameObject named "PlayerSpawn" or "SpawnPoint"
        GameObject namedSpawn = GameObject.Find("PlayerSpawn") ?? GameObject.Find("SpawnPoint");
        if (namedSpawn != null)
        {
            return namedSpawn.transform.position;
        }
        
        return null; // No specific spawn point found
    }

    void Update()
    {
        if (rb == null) return;

        // Update FPS counter
        UpdateFPS();

        // Decrease level timer
        if (levelTimer > 0f)
        {
            levelTimer = Mathf.Max(0f, levelTimer - Time.deltaTime);
        }

        // Update elapsed time in GameManager if available
        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateElapsedTime(Time.deltaTime);
        }

        if (GameObject.FindGameObjectsWithTag("Coin").Length == 0)
        {
            GameObject[] walls = GameObject.FindGameObjectsWithTag("BreakableWall");
            foreach (GameObject wall in walls)
            {
                Destroy(wall);
            }
        }

        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;

            blinkTimer -= Time.deltaTime;
            if (blinkTimer <= 0f)
            {
                isVisible = !isVisible;
                spriteRenderer.color = isVisible ? Color.white : new Color(1f, 1f, 1f, 0.3f);
                blinkTimer = blinkSpeed;
            }

            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                spriteRenderer.color = Color.white;
                Debug.Log("Invincibility ended");
            }
        }

        float moveInput = Input.GetAxis("Horizontal");
        rb.linearVelocity = new Vector2(moveInput * Speed, rb.linearVelocity.y);

        if (moveInput > 0.01f) spriteRenderer.flipX = false;
        else if (moveInput < -0.01f) spriteRenderer.flipX = true;

        if (Input.GetKeyDown(KeyCode.Space) && touchingGround)
        {
            jumpSound?.Play();
            rb.AddForce(new Vector2(0f, jumpForce), ForceMode2D.Impulse);
            touchingGround = false;
        }

        float horizontalSpeed = Mathf.Abs(rb.linearVelocity.x);
        anim.SetFloat("Speed", horizontalSpeed);

        bool isJumping = !touchingGround || Mathf.Abs(rb.linearVelocity.y) > 0.1f;
        anim.SetBool("isJumping", isJumping);

        fireballTimer -= Time.deltaTime;

        if (hasFirePower && Input.GetKeyDown(KeyCode.L) && fireballTimer <= 0f)
        {
            ShootFireball();
            fireballTimer = fireballCooldown;
        }

        if (Input.GetKeyDown(KeyCode.U))
        {
            showUI = !showUI;
        }

        // Debug toggle for hitboxes
        if (Input.GetKeyDown(KeyCode.H))
        {
            showHitboxes = !showHitboxes;
            Debug.Log($"Hitbox visualization: {(showHitboxes ? "ON" : "OFF")}");
        }

        // Debug: Save game state
        if (Input.GetKeyDown(KeyCode.F5) && GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameState(this);
            Debug.Log("Game state saved manually (F5)");
        }
        
        // Debug: Log checkpoint state
        if (Input.GetKeyDown(KeyCode.F6) && GameManager.Instance != null)
        {
            GameManager.Instance.currentGameState.LogState();
        }
    }

    // Made with AI to visualize hitboxes and debug info
    void OnDrawGizmos()
    {
        if (!showHitboxes || playerCollider == null) return;

        // Draw collider bounds
        Bounds bounds = playerCollider.bounds;

        // Set gizmo color based on ground contact
        Gizmos.color = touchingGround ? Color.green : Color.red;

        if (playerCollider is BoxCollider2D)
        {
            Gizmos.DrawWireCube(bounds.center, bounds.size);
        }
        else if (playerCollider is CircleCollider2D circleCollider)
        {
            Gizmos.DrawWireSphere(bounds.center, circleCollider.radius);
        }

        // Draw transform position
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, 0.1f);

        // Draw sprite bounds for comparison
        if (spriteRenderer != null && spriteRenderer.sprite != null)
        {
            Gizmos.color = Color.yellow;
            Bounds spriteBounds = spriteRenderer.bounds;
            Gizmos.DrawWireCube(spriteBounds.center, spriteBounds.size);
        }
    }

    //Made with AI to update FPS counter
    void UpdateFPS()
    {
        fpsTimeLeft -= Time.deltaTime;
        fpsAccumulator += Time.timeScale / Time.deltaTime;
        fpsFrames++;

        if (fpsTimeLeft <= 0f)
        {
            fps = fpsAccumulator / fpsFrames;
            fpsTimeLeft = fpsUpdateInterval;
            fpsAccumulator = 0f;
            fpsFrames = 0;
        }
    }

    public void GrowMario()
    {
        if (isGrown)
        {
            Debug.Log("Mario is already grown, skipping growth");
            return; // Already grown
        }

        if (showHitboxes)
        {
            Debug.Log($"BEFORE GROWTH - Position: {transform.position}, Collider bounds: {playerCollider.bounds}, touchingGround: {touchingGround}");
        }

        isGrown = true;

        // Store the bottom position before growth
        float bottomY = playerCollider.bounds.min.y;

        // Scale the entire transform (sprite)
        transform.localScale = originalScale * growthScale;

        // Adjust collider to match the scaled sprite bounds much more closely
        if (playerCollider != null)
        {
            if (playerCollider is BoxCollider2D boxCollider)
            {
                // Make collider match sprite bounds more closely
                // Use much smaller scaling to match the yellow rectangle better
                float colliderScale = 1.0f + (growthScale - 1.0f) * 0.01f; // Reduced from 0.7f to 0.3f
                boxCollider.size = originalColliderSize * colliderScale;
                
                // Keep the original offset
                boxCollider.offset = originalColliderOffset;
                
                // Adjust transform position to maintain the bottom position
                float newBottomY = playerCollider.bounds.min.y;
                float yAdjustment = bottomY - newBottomY;

                if (showHitboxes)
                {
                    Debug.Log($"Bottom was: {bottomY}, now: {newBottomY}, adjustment: {yAdjustment}");
                    Debug.Log($"Collider scale factor: {colliderScale} (instead of {growthScale})");
                    Debug.Log($"Sprite scale: {growthScale}, Collider scale: {colliderScale}");
                }

                Vector3 newPos = transform.position;
                newPos.y += yAdjustment;
                transform.position = newPos;
            }
            else if (playerCollider is CircleCollider2D circleCollider)
            {
                // Similar approach for circle collider
                float colliderScale = 1.0f + (growthScale - 1.0f) * 0.3f;
                float newRadius = (originalColliderSize.x / 2) * colliderScale;
                circleCollider.radius = newRadius;
                
                circleCollider.offset = originalColliderOffset;

                float newBottomY = playerCollider.bounds.min.y;
                float yAdjustment = bottomY - newBottomY;

                Vector3 newPos = transform.position;
                newPos.y += yAdjustment;
                transform.position = newPos;
            }
        }

        // Force recheck ground collision after size change
        StartCoroutine(RecheckGroundCollisionAfterGrowth());

        if (showHitboxes)
        {
            Debug.Log($"AFTER GROWTH - Position: {transform.position}, Collider bounds: {playerCollider.bounds}, touchingGround: {touchingGround}");
        }
        Debug.Log("Mario grew by " + ((growthScale - 1) * 100) + "% to scale " + transform.localScale);
    }

    public void ShrinkMario()
    {
        if (!isGrown) return; // Already normal size

        if (showHitboxes)
        {
            Debug.Log($"BEFORE SHRINK - Position: {transform.position}, Collider bounds: {playerCollider.bounds}, touchingGround: {touchingGround}");
        }

        // Store the bottom position before shrinking
        float bottomY = playerCollider.bounds.min.y;

        isGrown = false;

        // Reset to original scale
        transform.localScale = originalScale;

        // Reset collider to original size and offset
        if (playerCollider != null)
        {
            if (playerCollider is BoxCollider2D boxCollider)
            {
                boxCollider.size = originalColliderSize;
                boxCollider.offset = originalColliderOffset;

                // Adjust position to maintain bottom
                float newBottomY = playerCollider.bounds.min.y;
                float yAdjustment = bottomY - newBottomY;

                Vector3 newPos = transform.position;
                newPos.y += yAdjustment;
                transform.position = newPos;
            }
            else if (playerCollider is CircleCollider2D circleCollider)
            {
                circleCollider.radius = originalColliderSize.x / 2;
                circleCollider.offset = originalColliderOffset;

                float newBottomY = playerCollider.bounds.min.y;
                float yAdjustment = bottomY - newBottomY;

                Vector3 newPos = transform.position;
                newPos.y += yAdjustment;
                transform.position = newPos;
            }
        }

        // Force recheck ground collision after size change
        StartCoroutine(RecheckGroundCollisionAfterGrowth());

        if (showHitboxes)
        {
            Debug.Log($"AFTER SHRINK - Position: {transform.position}, Collider bounds: {playerCollider.bounds}, touchingGround: {touchingGround}");
        }
        Debug.Log("Mario returned to normal size and position!");
    }

    // Coroutine to recheck ground collision after size changes
    private System.Collections.IEnumerator RecheckGroundCollisionAfterGrowth()
    {
        // Wait a frame for physics to update
        yield return new WaitForFixedUpdate();
        
        // Check if we're touching ground using a raycast from the bottom of the collider
        if (playerCollider != null)
        {
            Bounds bounds = playerCollider.bounds;
            RaycastHit2D hit = Physics2D.Raycast(
                new Vector2(bounds.center.x, bounds.min.y), 
                Vector2.down, 
                0.1f
            );
            
            bool wasGrounded = touchingGround;
            touchingGround = hit.collider != null && hit.collider.CompareTag("Ground");
            
            if (showHitboxes && wasGrounded != touchingGround)
            {
                Debug.Log($"Ground state corrected after growth: {wasGrounded} -> {touchingGround}");
            }
        }
    }

    public void GiveFirePower()
    {
        hasFirePower = true;
        GrowMario();
        Debug.Log("Mario got fire power and grew!");
    }

    public void RemoveFirePower()
    {
        hasFirePower = false;
        ShrinkMario();
        Debug.Log("Mario lost fire power and shrunk!");
    }

    void ShootFireball()
    {
        if (fireballPrefab == null) return;

        // Determine facing direction: right = 1, left = -1
        float facing = spriteRenderer.flipX ? -1f : 1f;

        // Calculate spawn position dynamically based on player position and facing direction
        Vector3 spawnPosition = transform.position;
        spawnPosition.x += facing * 0.7f;
        spawnPosition.y += 0.3f;

        // If firePoint is assigned, use it as a base but still adjust for direction
        if (firePoint != null)
        {
            spawnPosition = firePoint.position;
            // Still adjust X position based on facing direction
            spawnPosition.x = transform.position.x + (facing * 0.7f);
        }

        // Instantiate fireball at the calculated position
        GameObject fireball = Instantiate(fireballPrefab, spawnPosition, Quaternion.identity);

        // Calculate velocity - increase horizontal speed to overcome bouncy physics
        Vector2 velocity = new Vector2(facing * fireballSpeed, fireballArc * fireballSpeed);

        // Set velocity directly on Rigidbody2D
        Rigidbody2D fbRb = fireball.GetComponent<Rigidbody2D>();
        if (fbRb != null)
        {
            // With bouncy physics material, we need to ensure the velocity is maintained
            fbRb.linearVelocity = velocity;

            // Disable rotation to prevent spinning issues with bouncy material
            fbRb.freezeRotation = true;
        }

        // Also use the Fireball script method
        Fireball fbScript = fireball.GetComponent<Fireball>();
        if (fbScript != null)
        {
            fbScript.Shoot(velocity);
        }

        // Flip fireball sprite to match player direction
        SpriteRenderer fbSprite = fireball.GetComponent<SpriteRenderer>();
        if (fbSprite != null)
        {
            fbSprite.flipX = spriteRenderer.flipX;
        }
    }

    void OnGUI()
    {
        if (!showUI) return;

        if (Lives <= 0)
        {
            DisplayGameOver();
            return;
        }

        // Coins + heart moved to the right side
        DisplayCoins();

        // Lives are now shown next to coins (DisplayLives not called here anymore)

        DisplayInstructions();
    }

    // All the UI drawing methods below were made with AI
    private void DisplayCoins()
    {
        GUIStyle textStyle = new GUIStyle(GUI.skin.label);
        textStyle.fontSize = 24;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = Color.yellow;

        float margin = 20f;
        float iconSize = 32f;
        float textWidth = 80f;
        float spacing = 8f;
        float lifeTextWidth = 60f; // width reserved for "x N" lives text

        // compute total UI width (coin icon + coin text + spacing + heart icon + life text + small paddings)
        float totalWidth = iconSize + textWidth + spacing + iconSize + lifeTextWidth + spacing;
        float startX = Screen.width - margin - totalWidth;
        Vector2 position = new Vector2(Mathf.Max(margin, startX), margin);

        // Draw coin icon
        if (coinSprite != null)
        {
            Texture2D coinTexture = coinSprite.texture;

            if (coinTexture != null)
            {
                Rect spriteRect = coinSprite.rect;
                Rect uvRect = new Rect(
                    spriteRect.x / coinTexture.width,
                    spriteRect.y / coinTexture.height,
                    spriteRect.width / coinTexture.width,
                    spriteRect.height / coinTexture.height
                );

                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTextureWithTexCoords(new Rect(position.x + 2, position.y + 2, iconSize, iconSize), coinTexture, uvRect);

                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(new Rect(position.x, position.y, iconSize, iconSize), coinTexture, uvRect);
            }
            else
            {
                DrawFallbackCoin(position, iconSize, textStyle);
            }
        }
        else
        {
            DrawFallbackCoin(position, iconSize, textStyle);
        }

        // Coin count text
        string coinText = "x " + coins_collected.ToString();

        GUI.color = Color.black;
        GUI.Label(new Rect(position.x + iconSize + 6, position.y + 6, textWidth, 30), coinText, textStyle);

        GUI.color = Color.yellow;
        GUI.Label(new Rect(position.x + iconSize + 5, position.y + 5, textWidth, 30), coinText, textStyle);

        // Draw a single heart icon next to the coin UI (moves heart next to coins)
        float heartX = position.x + iconSize + textWidth + spacing;
        Vector2 heartPosition = new Vector2(heartX, position.y);

        if (live != null)
        {
            Texture2D liveTexture = live.texture;
            if (liveTexture != null)
            {
                Rect spriteRect = live.rect;
                Rect uvRect = new Rect(
                    spriteRect.x / liveTexture.width,
                    spriteRect.y / liveTexture.height,
                    spriteRect.width / liveTexture.width,
                    spriteRect.height / liveTexture.height
                );

                GUI.color = new Color(0, 0, 0, 0.5f);
                GUI.DrawTextureWithTexCoords(new Rect(heartPosition.x + 2, heartPosition.y + 2, iconSize, iconSize), liveTexture, uvRect);

                GUI.color = Color.white;
                GUI.DrawTextureWithTexCoords(new Rect(heartPosition.x, heartPosition.y, iconSize, iconSize), liveTexture, uvRect);
            }
            else
            {
                DrawFallbackLife(heartPosition, iconSize);
            }
        }
        else
        {
            DrawFallbackLife(heartPosition, iconSize);
        }

        // Draw lives count next to the heart
        GUIStyle lifeCountStyle = new GUIStyle(GUI.skin.label);
        lifeCountStyle.fontSize = 20;
        lifeCountStyle.fontStyle = FontStyle.Bold;
        lifeCountStyle.normal.textColor = Color.red;

        string lifeText = "x " + Lives.ToString();
        // Ensure life label stays on-screen by using the computed layout
        float lifeTextX = heartPosition.x + iconSize + 6;
        GUI.color = Color.black;
        GUI.Label(new Rect(lifeTextX, heartPosition.y + 6, lifeTextWidth, 30), lifeText, lifeCountStyle);
        GUI.color = Color.red;
        GUI.Label(new Rect(lifeTextX - 1, heartPosition.y + 5, lifeTextWidth, 30), lifeText, lifeCountStyle);

        // Draw level timer below the coins/heart UI
        GUIStyle timerStyle = new GUIStyle(GUI.skin.label);
        timerStyle.fontSize = 18;
        timerStyle.normal.textColor = Color.white;

        int remaining = Mathf.Max(0, Mathf.CeilToInt(levelTimer));
        int minutes = remaining / 60;
        int seconds = remaining % 60;
        string timerText = string.Format("{0:D2}:{1:D2}", minutes, seconds);

        float timerY = position.y + iconSize + 6;
        float timerWidth = 140f;

        GUI.color = Color.black;
        GUI.Label(new Rect(position.x + iconSize + 4, timerY + 1, timerWidth, 24), timerText, timerStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(position.x + iconSize + 3, timerY, timerWidth, 24), timerText, timerStyle);

        // Draw FPS counter below the timer
        GUIStyle fpsStyle = new GUIStyle(GUI.skin.label);
        fpsStyle.fontSize = 16;
        fpsStyle.normal.textColor = Color.green;

        string fpsText = "FPS: " + Mathf.RoundToInt(fps).ToString();
        float fpsY = timerY + 26;
        float fpsWidth = 80f;

        GUI.color = Color.black;
        GUI.Label(new Rect(position.x + iconSize + 4, fpsY + 1, fpsWidth, 20), fpsText, fpsStyle);
        GUI.color = Color.green;
        GUI.Label(new Rect(position.x + iconSize + 3, fpsY, fpsWidth, 20), fpsText, fpsStyle);

        GUI.color = Color.white;
    }

    private void DisplayLives()
    {
        // kept for compatibility but not used in OnGUI anymore
        float margin = 20f;
        float iconSize = 32f;
        float spacing = 5f;

        Vector2 startPosition = new Vector2(margin, margin);

        for (int i = 0; i < Lives; i++)
        {
            Vector2 lifePosition = new Vector2(startPosition.x + (iconSize + spacing) * i, startPosition.y);

            if (live != null)
            {
                Texture2D liveTexture = live.texture;

                if (liveTexture != null)
                {
                    Rect spriteRect = live.rect;
                    Rect uvRect = new Rect(
                        spriteRect.x / liveTexture.width,
                        spriteRect.y / liveTexture.height,
                        spriteRect.width / liveTexture.width,
                        spriteRect.height / liveTexture.height
                    );

                    GUI.color = new Color(0, 0, 0, 0.5f);
                    GUI.DrawTextureWithTexCoords(new Rect(lifePosition.x + 2, lifePosition.y + 2, iconSize, iconSize), liveTexture, uvRect);

                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(lifePosition.x, lifePosition.y, iconSize, iconSize), liveTexture, uvRect);
                }
                else
                {
                    DrawFallbackLife(lifePosition, iconSize);
                }
            }
            else
            {
                DrawFallbackLife(lifePosition, iconSize);
            }
        }

        GUI.color = Color.white;
    }

    private void DrawFallbackLife(Vector2 position, float iconSize)
    {
        GUIStyle lifeStyle = new GUIStyle(GUI.skin.label);
        lifeStyle.fontSize = (int)(iconSize * 0.8f);
        lifeStyle.alignment = TextAnchor.MiddleCenter;
        lifeStyle.normal.textColor = Color.red;

        GUI.color = Color.black;
        GUI.Label(new Rect(position.x + 1, position.y + 1, iconSize, iconSize), "♥", lifeStyle);

        GUI.color = Color.red;
        GUI.Label(new Rect(position.x, position.y, iconSize, iconSize), "♥", lifeStyle);
    }

    private void DisplayInstructions()
    {
        GUIStyle instructionStyle = new GUIStyle(GUI.skin.label);
        instructionStyle.fontSize = 12;
        instructionStyle.normal.textColor = Color.white;

        string instructions = "Press 'U' to toggle UI, 'H' to toggle hitboxes, 'F6' to log checkpoint state";
        GUI.color = Color.black;
        GUI.Label(new Rect(11, Screen.height - 31, 400, 20), instructions, instructionStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(10, Screen.height - 30, 400, 20), instructions, instructionStyle);
    }

    private void DrawFallbackCoin(Vector2 position, float iconSize, GUIStyle textStyle)
    {
        GUIStyle coinStyle = new GUIStyle(textStyle);
        coinStyle.fontSize = (int)(iconSize * 0.8f);
        coinStyle.alignment = TextAnchor.MiddleCenter;

        GUI.color = Color.black;
        GUI.Label(new Rect(position.x + 1, position.y + 1, iconSize, iconSize), "●", coinStyle);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(position.x, position.y, iconSize, iconSize), "●", coinStyle);
    }

    private void TakeDamage()
    {
        if (isInvincible) return;

        // If Mario has fire power, lose the power-up instead of a life
        if (hasFirePower)
        {
            RemoveFirePower();
            Debug.Log("Mario lost fire power due to damage!");
            
            // Start invincibility after losing power-up
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
            blinkTimer = blinkSpeed;
            Debug.Log("Mario is now invincible for " + invincibilityDuration + " seconds");
            return; // Don't lose a life, just the power-up
        }

        // If Mario doesn't have fire power, lose a life as normal
        Lives--;
        Debug.Log("Player took damage! Lives remaining: " + Lives);

        if (Lives > 0)
        {
            // Just start invincibility - no respawn on damage
            isInvincible = true;
            invincibilityTimer = invincibilityDuration;
            blinkTimer = blinkSpeed;
            Debug.Log("Player is now invincible for " + invincibilityDuration + " seconds");
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            touchingGround = true;
            if (showHitboxes)
            {
                Debug.Log($"Ground collision detected - touchingGround set to TRUE. Mario grown: {isGrown}");
            }
        }
        if (collision.gameObject.CompareTag("Enemy"))
        {
            TakeDamage();
        }
        // FIXED: Handle enemy projectiles (boss projectiles)
        if (collision.gameObject.CompareTag("EnemyProjectile"))
        {
            Debug.Log("Player hit by enemy projectile!");
            TakeDamage();
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            touchingGround = false;
            if (showHitboxes)
            {
                Debug.Log($"Ground collision lost - touchingGround set to FALSE. Mario grown: {isGrown}");
            }
        }
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            touchingGround = true;
            // This helps ensure we stay grounded even during size changes
        }
    }

    private void DisplayGameOver()
    {
        rb.linearVelocity = Vector2.zero;
        rb.isKinematic = true;
        anim.SetFloat("Speed", 0);
        anim.SetBool("isJumping", false);
        showUI = true;

        GUIStyle gameOverStyle = new GUIStyle(GUI.skin.label);
        gameOverStyle.fontSize = 48;
        gameOverStyle.fontStyle = FontStyle.Bold;
        gameOverStyle.alignment = TextAnchor.MiddleCenter;
        gameOverStyle.normal.textColor = Color.red;
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 151, Screen.height / 2 - 51, 302, 102), "GAME OVER", gameOverStyle);

        GUI.color = Color.red;
        GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 - 50, 300, 100), "GAME OVER", gameOverStyle);

        GUIStyle restartStyle = new GUIStyle(GUI.skin.label);
        restartStyle.fontSize = 18;
        restartStyle.alignment = TextAnchor.MiddleCenter;
        restartStyle.normal.textColor = Color.white;

        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 149, Screen.height / 2 + 51, 302, 30), "Press R to restart", restartStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height / 2 + 50, 300, 30), "Press R to restart", restartStyle);

        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartGame();
        }
    }

    private void RestartGame()
    {
        Lives = 3;
        coins_collected = 0;
        isInvincible = false;
        spriteRenderer.color = Color.white;
        levelTimer = 180f;

        // Reset fire power and size
        hasFirePower = false;
        ShrinkMario();

        // Try to respawn at checkpoint instead of level start
        bool respawnedAtCheckpoint = RespawnAtCheckpointForRestart();
        
        if (!respawnedAtCheckpoint)
        {
            Debug.Log("Game restarted at level start!");
        }
        else
        {
            Debug.Log("Game restarted at checkpoint!");
        }
        
        // Reset physics
        rb.isKinematic = false;
        rb.linearVelocity = Vector2.zero;
    }
    
    // Method specifically for restart - returns true if respawned at checkpoint
    private bool RespawnAtCheckpointForRestart()
    {
        // First check GameManager for persistent checkpoint data
        if (GameManager.Instance != null && GameManager.Instance.currentGameState.hasActiveCheckpoint)
        {
            GameState state = GameManager.Instance.currentGameState;
            
            // For restart scenarios, we want to use the checkpoint regardless of which scene we're currently in
            // This allows cross-scene respawning (e.g., checkpoint in Scene 1, die in Boss scene, respawn at Scene 1 checkpoint)
            if (state.checkpointPosition != Vector3.zero)
            {
                // If checkpoint is from a different scene, we should transition back to that scene
                string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                
                if (!string.IsNullOrEmpty(state.checkpointScene) && state.checkpointScene != currentScene)
                {
                    // Transition back to the checkpoint scene
                    Debug.Log($"Checkpoint is in different scene ({state.checkpointScene}), transitioning back...");
                    
                    if (GameManager.Instance != null)
                    {
                        // Use GameManager to transition to checkpoint scene with checkpoint position
                        GameManager.Instance.TransitionToScene(state.checkpointScene, state.checkpointPosition);
                        return true;
                    }
                }
                else
                {
                    // Checkpoint is in current scene, just set position
                    SetPosition(state.checkpointPosition);
                    Debug.Log($"Respawned at persistent checkpoint: {state.checkpointPosition}");
                    return true;
                }
            }
        }
        
        // Fallback: Try CheckpointManager for current scene
        if (CheckpointManager.Instance != null)
        {
            FlagPole activeCheckpoint = CheckpointManager.Instance.GetActiveCheckpoint();
            if (activeCheckpoint != null)
            {
                activeCheckpoint.RespawnPlayerAtCheckpoint();
                return true;
            }
        }
        
        // Fallback: Search for active checkpoints manually in current scene
        FlagPole[] checkpoints = FindObjectsOfType<FlagPole>();
        foreach (FlagPole checkpoint in checkpoints)
        {
            if (checkpoint.ShouldUseForRespawn())
            {
                checkpoint.RespawnPlayerAtCheckpoint();
                return true;
            }
        }
        
        return false; // No checkpoint found, will start at level beginning
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Coin"))
        {
            coins_collected++;
            Debug.Log("Coin collected! Total: " + coins_collected);
        }
    }

    public int GetCoinsCollected()
    {
        return coins_collected;
    }

    public void AddCoins(int amount)
    {
        coins_collected += (short)amount;
    }

    public bool IsInvincible()
    {
        return isInvincible;
    }

    public void SetInvincible(bool invincible)
    {
        isInvincible = invincible;
        if (!invincible)
        {
            spriteRenderer.color = Color.white;
        }
    }

    // New getter for grown state (needed for GameManager)
    public bool IsGrown()
    {
        return isGrown;
    }

    // Method to manually save state to GameManager
    public void SaveToGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveGameState(this);
            Debug.Log("Player state manually saved to GameManager");
        }
        else
        {
            Debug.LogWarning("GameManager not found - cannot save state");
        }
    }

    // Method to manually load state from GameManager
    public void LoadFromGameManager()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadGameState(this);
            Debug.Log("Player state manually loaded from GameManager");
        }
        else
        {
            Debug.LogWarning("GameManager not found - cannot load state");
        }
    }

    // Method to manually trigger scene transition with state saving
    public void TransitionToScene(string sceneName, Vector3? spawnPosition = null)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TransitionToScene(sceneName, spawnPosition);
        }
        else
        {
            Debug.LogError("GameManager not found - cannot transition to scene");
        }
    }

    // Method to set position manually (useful for teleports within same scene)
    public void SetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        Debug.Log($"Player position set to: {newPosition}");
    }
}