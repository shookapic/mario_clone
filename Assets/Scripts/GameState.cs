using UnityEngine;

[System.Serializable]
public class GameState
{
    [Header("Player Stats")]
    public int coins = 0;
    public int lives = 3;
    public float elapsedTime = 0f;
    public bool hasFirePower = false;
    public bool isGrown = false;

    [Header("Level Progress")]
    public string currentScene = "";
    public Vector3 playerPosition = Vector3.zero;
    public bool useCustomSpawnPosition = false;
    public string targetSpawnId = "";
    
    [Header("Checkpoint System")]
    public Vector3 checkpointPosition = Vector3.zero;
    public bool hasActiveCheckpoint = false;
    public string checkpointScene = "";

    [Header("Game Settings")]
    public bool showUI = true;
    public float gameVersion = 1.0f;

    // Constructor
    public GameState()
    {
        // Default values are set above
    }

    // Copy constructor to create a copy of current state
    public GameState(GameState other)
    {
        coins = other.coins;
        lives = other.lives;
        elapsedTime = other.elapsedTime;
        hasFirePower = other.hasFirePower;
        isGrown = other.isGrown;
        currentScene = other.currentScene;
        playerPosition = other.playerPosition;
        useCustomSpawnPosition = other.useCustomSpawnPosition;
        targetSpawnId = other.targetSpawnId;
        checkpointPosition = other.checkpointPosition;
        hasActiveCheckpoint = other.hasActiveCheckpoint;  
        checkpointScene = other.checkpointScene;
        showUI = other.showUI;
        gameVersion = other.gameVersion;
    }

    // Reset to default values
    public void Reset()
    {
        coins = 0;
        lives = 3;
        elapsedTime = 0f;
        hasFirePower = false;
        isGrown = false;
        currentScene = "";
        playerPosition = Vector3.zero;
        useCustomSpawnPosition = false;
        targetSpawnId = "";
        checkpointPosition = Vector3.zero;
        hasActiveCheckpoint = false;
        checkpointScene = "";
        showUI = true;
    }

    // Debug method to log current state
    public void LogState()
    {
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        Debug.Log($"GameState - Coins: {coins}, Lives: {lives}, Time: {elapsedTime:F1}s, Fire Power: {hasFirePower}, Grown: {isGrown}");
        Debug.Log($"Current Scene: {currentScene}, Custom Spawn: {useCustomSpawnPosition}, Spawn ID: {targetSpawnId}");
        Debug.Log($"Checkpoint: {(hasActiveCheckpoint ? "ACTIVE" : "NONE")} at {checkpointPosition} in scene '{checkpointScene}'");
        
        if (hasActiveCheckpoint && checkpointScene != currentScene)
        {
            Debug.Log($"NOTE: Checkpoint is in different scene! Current: '{currentScene}', Checkpoint: '{checkpointScene}'");
        }
    }
    
    // Method to clear checkpoint data (useful when starting new levels)
    public void ClearCheckpoint()
    {
        checkpointPosition = Vector3.zero;
        hasActiveCheckpoint = false;
        checkpointScene = "";
        Debug.Log("Checkpoint data cleared");
    }
}