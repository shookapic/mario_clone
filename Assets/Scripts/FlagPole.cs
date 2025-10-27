using UnityEngine;

public class FlagPole : MonoBehaviour
{
    [Header("Checkpoint Settings")]
    public bool isCheckpointActivated = false;
    public bool isLevelEndFlag = false; // Set true if this should complete the level
    
    [Header("Animation Settings")]
    public Animator flagAnimator;
    public string activationAnimationTrigger = "Activate";
    public AudioClip checkpointSound;
    
    [Header("Visual Feedback")]
    public ParticleSystem activationParticles;
    public Color activatedColor = Color.green;
    public Color defaultColor = Color.white;
    
    private bool hasBeenActivated = false;
    private AudioSource audioSource;
    private SpriteRenderer spriteRenderer;
    private Vector3 checkpointPosition;
    
    // Win-related variables (keep for level-end flags)
    private bool showWinMessage = false;
    
    void Start()
    {
        // Get components
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        spriteRenderer = GetComponent<SpriteRenderer>();
        
        // Set checkpoint position to this flag's position
        checkpointPosition = transform.position;
        
        // Set initial color
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
    }

    void Update()
    {
        // Handle restart input for level-end flags
        if (isLevelEndFlag && showWinMessage && Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (isLevelEndFlag && !hasBeenActivated)
            {
                CompleteLevel();
            }
            else if (!hasBeenActivated)
            {
                ActivateCheckpoint(collision.gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (isLevelEndFlag && !hasBeenActivated)
            {
                CompleteLevel();
            }
            else if (!hasBeenActivated)
            {
                ActivateCheckpoint(collision.gameObject);
            }
        }
    }

    private void ActivateCheckpoint(GameObject player)
    {
        if (hasBeenActivated) return;
        
        hasBeenActivated = true;
        isCheckpointActivated = true;
        
        // Store checkpoint position (use player's current position for more accurate respawn)
        checkpointPosition = player.transform.position;
        
        // Register with CheckpointManager if available
        if (CheckpointManager.Instance != null)
        {
            CheckpointManager.Instance.RegisterActiveCheckpoint(this);
        }
        
        // Save checkpoint to GameManager if available
        if (GameManager.Instance != null)
        {
            // Save checkpoint data using the new persistent fields
            GameState state = GameManager.Instance.currentGameState;
            
            if (state != null)
            {
                // Set checkpoint data that persists across scenes and restarts
                state.checkpointPosition = checkpointPosition;
                state.hasActiveCheckpoint = true;
                state.checkpointScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
                
                // Save additional player state
                MyPlayerMovement playerMovement = player.GetComponent<MyPlayerMovement>();
                if (playerMovement != null)
                {
                    state.hasFirePower = playerMovement.hasFirePower;
                    state.isGrown = playerMovement.isGrown;
                    state.lives = playerMovement.Lives;
                    state.coins = playerMovement.GetCoinsCollected();
                }
            }
            
            Debug.Log($"Checkpoint activated at position: {checkpointPosition}");
        }
        
        // Play activation animation
        if (flagAnimator != null && !string.IsNullOrEmpty(activationAnimationTrigger))
        {
            flagAnimator.SetTrigger(activationAnimationTrigger);
        }
        
        // Play sound effect
        if (audioSource != null && checkpointSound != null)
        {
            audioSource.clip = checkpointSound;
            audioSource.Play();
        }
        
        // Spawn particles
        if (activationParticles != null)
        {
            activationParticles.Play();
        }
        
        // Change color to indicate activation
        if (spriteRenderer != null)
        {
            spriteRenderer.color = activatedColor;
        }
        
        ShowCheckpointMessage();
    }

    private void ShowCheckpointMessage()
    {
        Debug.Log("Checkpoint Activated!");
    }

    private void CompleteLevel()
    {
        hasBeenActivated = true;
        showWinMessage = true;
        
        // Play activation effects for level completion too
        if (flagAnimator != null && !string.IsNullOrEmpty(activationAnimationTrigger))
        {
            flagAnimator.SetTrigger(activationAnimationTrigger);
        }
        
        if (audioSource != null && checkpointSound != null)
        {
            audioSource.clip = checkpointSound;
            audioSource.Play();
        }
        
        if (activationParticles != null)
        {
            activationParticles.Play();
        }
    }

    // Method to respawn player at this checkpoint
    public void RespawnPlayerAtCheckpoint()
    {
        if (!isCheckpointActivated) return;
        
        MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
        if (player != null)
        {
            player.SetPosition(checkpointPosition);
            
            // Restore player state from GameManager if available
            if (GameManager.Instance != null)
            {
                GameState state = GameManager.Instance.currentGameState;
                
                // Only restore power-ups and growth state, not lives/coins (those are handled by restart logic)
                player.hasFirePower = state.hasFirePower;
                
                // Handle growth state restoration
                if (state.isGrown && !player.IsGrown())
                {
                    player.GrowMario();
                }
                else if (!state.isGrown && player.IsGrown())
                {
                    player.ShrinkMario();
                }
                
                Debug.Log($"Player respawned at checkpoint: {checkpointPosition} with restored state");
            }
            else
            {
                Debug.Log($"Player respawned at checkpoint: {checkpointPosition}");
            }
        }
    }

    // Method to check if this checkpoint should be used for respawn
    public bool ShouldUseForRespawn()
    {
        return isCheckpointActivated && !isLevelEndFlag;
    }

    // Get checkpoint position
    public Vector3 GetCheckpointPosition()
    {
        return checkpointPosition;
    }

    // Made with AI
    void OnGUI()
    {
        if (!showWinMessage) return;

        GUIStyle winStyle = new GUIStyle(GUI.skin.label);
        winStyle.fontSize = 56;
        winStyle.fontStyle = FontStyle.Bold;
        winStyle.alignment = TextAnchor.MiddleCenter;
        winStyle.normal.textColor = Color.green;

        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 201, Screen.height / 2 - 81, 402, 162), "YOU WON!", winStyle);
        
        GUI.color = Color.green;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 - 80, 400, 160), "YOU WON!", winStyle);

        GUIStyle celebrationStyle = new GUIStyle(GUI.skin.label);
        celebrationStyle.fontSize = 20;
        celebrationStyle.alignment = TextAnchor.MiddleCenter;
        celebrationStyle.normal.textColor = Color.yellow;

        string celebrationText = "🎉 Congratulations! Level Complete! 🎉";
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 199, Screen.height / 2 + 21, 402, 30), celebrationText, celebrationStyle);
        GUI.color = Color.yellow;
        GUI.Label(new Rect(Screen.width / 2 - 200, Screen.height / 2 + 20, 400, 30), celebrationText, celebrationStyle);

        GUIStyle restartStyle = new GUIStyle(GUI.skin.label);
        restartStyle.fontSize = 16;
        restartStyle.alignment = TextAnchor.MiddleCenter;
        restartStyle.normal.textColor = Color.white;
        
        string restartText = "Press 'R' to restart level";
        GUI.color = Color.black;
        GUI.Label(new Rect(Screen.width / 2 - 101, Screen.height / 2 + 61, 202, 20), restartText, restartStyle);
        GUI.color = Color.white;
        GUI.Label(new Rect(Screen.width / 2 - 100, Screen.height / 2 + 60, 200, 20), restartText, restartStyle);
    }

    private void RestartLevel()
    {
        hasBeenActivated = false;
        isCheckpointActivated = false;
        showWinMessage = false;
        
        if (spriteRenderer != null)
            spriteRenderer.color = defaultColor;
            
        // Reset GameManager checkpoint state (only if this is a level-end flag)
        if (GameManager.Instance != null && isLevelEndFlag)
        {
            GameManager.Instance.currentGameState.useCustomSpawnPosition = false;
            GameManager.Instance.currentGameState.hasActiveCheckpoint = false;
            GameManager.Instance.currentGameState.checkpointPosition = Vector3.zero;
            GameManager.Instance.currentGameState.checkpointScene = "";
        }
    }

    // Public methods for external access (backward compatibility)
    public bool IsLevelCompleted()
    {
        return isLevelEndFlag && hasBeenActivated;
    }

    public bool IsCheckpointActive()
    {
        return isCheckpointActivated;
    }

    public void ManualActivateCheckpoint()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            ActivateCheckpoint(player);
        }
    }

    // Debug visualization in Scene view
    void OnDrawGizmos()
    {
        Vector3 pos = transform.position;
        
        if (isLevelEndFlag)
        {
            // Draw level end flag visualization
            Gizmos.color = Color.gold;
            Gizmos.DrawWireCube(pos, Vector3.one * 0.8f);
            Gizmos.DrawSphere(pos, 0.3f);
        }
        else
        {
            // Draw checkpoint visualization
            Gizmos.color = isCheckpointActivated ? activatedColor : defaultColor;
            Gizmos.DrawWireCube(pos, Vector3.one * 0.6f);
            Gizmos.DrawSphere(pos, 0.2f);
        }
        
        #if UNITY_EDITOR
        string label = isLevelEndFlag ? "Level End" : (isCheckpointActivated ? "Checkpoint (Active)" : "Checkpoint");
        UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, label);
        #endif
    }
}
