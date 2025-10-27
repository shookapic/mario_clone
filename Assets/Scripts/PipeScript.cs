using UnityEngine;
using UnityEngine.SceneManagement;

public class PipeScript : MonoBehaviour
{
    [Tooltip("Name of the scene to load. Make sure the scene is added to Build Settings.")]
    public string sceneToLoad;

    [Tooltip("Tag used to identify the player GameObject (default: Player).")]
    public string playerTag = "Player";

    [Header("Spawn Configuration")]
    [Tooltip("Optional spawn position in the target scene. If not set, will use spawn points.")]
    public Vector3 targetSpawnPosition = Vector3.zero;

    [Tooltip("Use custom spawn position instead of spawn points")]
    public bool useCustomSpawnPosition = false;
    
    [Tooltip("ID of the spawn point to use in target scene (leave empty for default)")]
    public string targetSpawnId = "";

    [Header("Visual Feedback")]
    [Tooltip("Show UI hint when player can enter pipe")]
    public bool showEnterHint = true;

    [Tooltip("Text to display when player can enter pipe")]
    public string enterHintText = "Press S or ? to enter pipe";

    // tracks whether the player is inside the pipe trigger
    private bool playerInside = false;

    void Update()
    {
        // If the player is inside the trigger and presses S or DownArrow, load the target scene
        if (playerInside && !string.IsNullOrEmpty(sceneToLoad))
        {
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
            {
                EnterPipe();
            }
        }
    }

    private void EnterPipe()
    {
        // Find the player to save their state
        GameObject playerObj = GameObject.FindWithTag(playerTag);
        if (playerObj == null)
        {
            Debug.LogWarning($"PipeScript: Could not find player with tag '{playerTag}'");
            // Still transition to scene even if player not found
            SceneManager.LoadScene(sceneToLoad);
            return;
        }

        MyPlayerMovement player = playerObj.GetComponent<MyPlayerMovement>();
        
        // Use GameManager if available, otherwise fallback to direct scene loading
        if (GameManager.Instance != null)
        {
            Debug.Log($"PipeScript: Using GameManager to transition to {sceneToLoad}");
            
            // Save current player state
            if (player != null)
            {
                GameManager.Instance.SaveGameState(player);
            }
            
            // Determine spawn configuration
            Vector3? spawnPos = null;
            string spawnId = null;
            
            if (useCustomSpawnPosition)
            {
                spawnPos = targetSpawnPosition;
                Debug.Log($"Using custom spawn position: {targetSpawnPosition}");
            }
            else if (!string.IsNullOrEmpty(targetSpawnId))
            {
                spawnId = targetSpawnId;
                Debug.Log($"Looking for spawn point with ID: {targetSpawnId}");
            }
            else
            {
                Debug.Log("Will use default spawn point in target scene");
            }
            
            // Transition with spawn configuration
            GameManager.Instance.TransitionToScene(sceneToLoad, spawnPos, spawnId);
        }
        else
        {
            Debug.LogWarning("PipeScript: GameManager not found, using direct scene loading (state will not be saved)");
            SceneManager.LoadScene(sceneToLoad);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = true;
            Debug.Log($"PipeScript: Player entered pipe trigger. Press S or ? to go to {sceneToLoad}");
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            playerInside = false;
            Debug.Log("PipeScript: Player left pipe trigger");
        }
    }

    //All UI drawing made with AI
    void OnGUI()
    {
        if (playerInside && showEnterHint && !string.IsNullOrEmpty(enterHintText))
        {
            // Create style for the hint text
            GUIStyle hintStyle = new GUIStyle(GUI.skin.label);
            hintStyle.fontSize = 16;
            hintStyle.fontStyle = FontStyle.Bold;
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.normal.textColor = Color.white;

            // Calculate position (center-bottom of screen)
            float width = 300f;
            float height = 30f;
            float x = (Screen.width - width) / 2;
            float y = Screen.height - 100f;

            // Draw with shadow effect
            GUI.color = Color.black;
            GUI.Label(new Rect(x + 2, y + 2, width, height), enterHintText, hintStyle);
            
            GUI.color = Color.white;
            GUI.Label(new Rect(x, y, width, height), enterHintText, hintStyle);
        }
    }

    // Helper method to set custom spawn position from inspector or other scripts
    public void SetTargetSpawnPosition(Vector3 position)
    {
        targetSpawnPosition = position;
        useCustomSpawnPosition = true;
    }

    // Helper method to set target spawn point ID
    public void SetTargetSpawnId(string spawnId)
    {
        targetSpawnId = spawnId;
        useCustomSpawnPosition = false;
    }

    // Draw gizmos to visualize pipe destination
    void OnDrawGizmos()
    {
        if (useCustomSpawnPosition)
        {
            // Draw a line from this pipe to the target spawn position
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
            // If we could somehow know the target position, we'd draw a line
            // For now, just show this is a custom spawn pipe
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position + Vector3.up, 0.3f);
        }
        else if (!string.IsNullOrEmpty(targetSpawnId))
        {
            // Show this pipe targets a specific spawn ID
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
        else
        {
            // Default pipe
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw additional info when selected
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 1.0f);
        
        #if UNITY_EDITOR
        string label = $"Pipe to: {sceneToLoad}";
        if (useCustomSpawnPosition)
        {
            label += $"\nCustom spawn: {targetSpawnPosition}";
        }
        else if (!string.IsNullOrEmpty(targetSpawnId))
        {
            label += $"\nSpawn ID: {targetSpawnId}";
        }
        UnityEditor.Handles.Label(transform.position + Vector3.up, label);
        #endif
    }

    // Context menu for testing
    [ContextMenu("Test Pipe Transition")]
    public void TestPipeTransition()
    {
        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            EnterPipe();
        }
        else
        {
            Debug.LogError("PipeScript: Cannot test transition - sceneToLoad is empty");
        }
    }
}
