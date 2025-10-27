using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Game State")]
    public GameState currentGameState;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool resetStateOnStart = false;
    
    [Header("Persistence")]
    public bool saveToDisk = true;
    public string saveFileName = "mario_save.json";

    [Header("Scene Transition")]
    [Tooltip("Preferred spawn point ID to use when transitioning to scenes")]
    public string preferredSpawnId = "default";

    public static GameManager Instance { get; private set; }

    public static System.Action<GameState> OnGameStateChanged;
    public static System.Action<string> OnSceneTransition;
    public static System.Action OnGameStateLoaded;

    private string previousSceneName = "";

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
            
            // Initialize game state
            if (currentGameState == null)
            {
                currentGameState = new GameState();
            }
            
            // Load from disk if enabled
            if (saveToDisk)
            {
                LoadGameStateFromDisk();
            }
            
            if (enableDebugLogs)
            {
                Debug.Log("GameManager initialized and set to persist across scenes");
            }
        }
        else
        {
            // Destroy duplicate GameManager
            if (enableDebugLogs)
            {
                Debug.Log("Duplicate GameManager destroyed");
            }
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (resetStateOnStart)
        {
            ResetGameState();
        }
        
        // Update current scene in game state
        currentGameState.currentScene = SceneManager.GetActiveScene().name;
        
        if (enableDebugLogs)
        {
            currentGameState.LogState();
        }
    }

    #region Game State Management

    public void SaveGameState(MyPlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot save game state - player is null");
            return;
        }

        // Save player stats
        currentGameState.coins = player.GetCoinsCollected();
        currentGameState.lives = player.Lives;
        currentGameState.hasFirePower = player.hasFirePower;
        currentGameState.isGrown = player.IsGrown();
        
        // Save player position (but this will be overridden by spawn points if they exist)
        currentGameState.playerPosition = player.transform.position;
        
        // Save current scene
        currentGameState.currentScene = SceneManager.GetActiveScene().name;

        // Save to disk if enabled
        if (saveToDisk)
        {
            SaveGameStateToDisk();
        }

        if (enableDebugLogs)
        {
            Debug.Log("Game state saved:");
            currentGameState.LogState();
        }

        // Notify other scripts that game state changed
        OnGameStateChanged?.Invoke(currentGameState);
    }

    public void LoadGameState(MyPlayerMovement player)
    {
        if (player == null)
        {
            Debug.LogWarning("Cannot load game state - player is null");
            return;
        }

        // Load player stats
        player.Lives = (short)currentGameState.lives;
        
        // Set coins to the saved amount
        int currentCoins = player.GetCoinsCollected();
        if (currentCoins != currentGameState.coins)
        {
            player.AddCoins(currentGameState.coins - currentCoins);
        }
        
        // Load power-up state
        if (currentGameState.hasFirePower)
        {
            if (!player.hasFirePower)
            {
                player.GiveFirePower();
            }
        }
        else if (currentGameState.isGrown)
        {
            if (!player.IsGrown())
            {
                player.GrowMario();
            }
        }
        else
        {
            // Make sure Mario is normal size
            if (player.hasFirePower)
            {
                player.RemoveFirePower();
            }
            else if (player.IsGrown())
            {
                player.ShrinkMario();
            }
        }

        if (enableDebugLogs)
        {
            Debug.Log("Game state loaded (stats only, position handled separately):");
            currentGameState.LogState();
        }
        
        // Notify that game state was loaded
        OnGameStateLoaded?.Invoke();
    }

    // New method to find appropriate spawn point
    public Vector3? FindSpawnPosition(string spawnId = null)
    {
        string targetSpawnId = spawnId ?? preferredSpawnId;
        
        // Get all spawn points in current scene
        SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
        
        if (spawnPoints.Length == 0)
        {
            if (enableDebugLogs)
            {
                Debug.Log("No spawn points found in scene");
            }
            return null;
        }
        
        // First, try to find a spawn point with matching ID and source scene
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.MatchesId(targetSpawnId) && sp.ShouldUseForScene(previousSceneName))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Found matching spawn point: {sp.spawnPointId} for source scene: {previousSceneName}");
                }
                return sp.GetSpawnPosition();
            }
        }
        
        // Second, try to find any spawn point with matching ID
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.MatchesId(targetSpawnId))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Found spawn point with matching ID: {sp.spawnPointId}");
                }
                return sp.GetSpawnPosition();
            }
        }
        
        // Fallback: use first spawn point that accepts the source scene
        foreach (SpawnPoint sp in spawnPoints)
        {
            if (sp.ShouldUseForScene(previousSceneName))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Using fallback spawn point: {sp.spawnPointId}");
                }
                return sp.GetSpawnPosition();
            }
        }
        
        // Last resort: use any spawn point
        if (enableDebugLogs)
        {
            Debug.Log($"Using first available spawn point: {spawnPoints[0].spawnPointId}");
        }
        return spawnPoints[0].GetSpawnPosition();
    }

    public void UpdateElapsedTime(float deltaTime)
    {
        currentGameState.elapsedTime += deltaTime;
    }

    public void ResetGameState()
    {
        currentGameState.Reset();
        
        // Also clear saved file
        if (saveToDisk)
        {
            DeleteSaveFile();
        }
        
        if (enableDebugLogs)
        {
            Debug.Log("Game state reset to defaults");
        }
        
        OnGameStateChanged?.Invoke(currentGameState);
    }

    #endregion

    #region Persistent Storage

    private void SaveGameStateToDisk()
    {
        try
        {
            string json = JsonUtility.ToJson(currentGameState, true);
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
            System.IO.File.WriteAllText(filePath, json);
            
            if (enableDebugLogs)
            {
                Debug.Log($"Game state saved to: {filePath}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save game state to disk: {e.Message}");
        }
    }

    private void LoadGameStateFromDisk()
    {
        try
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
            
            if (System.IO.File.Exists(filePath))
            {
                string json = System.IO.File.ReadAllText(filePath);
                GameState loadedState = JsonUtility.FromJson<GameState>(json);
                
                if (loadedState != null)
                {
                    currentGameState = loadedState;
                    
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Game state loaded from: {filePath}");
                        currentGameState.LogState();
                    }
                }
            }
            else if (enableDebugLogs)
            {
                Debug.Log("No save file found, using default game state");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load game state from disk: {e.Message}");
        }
    }

    private void DeleteSaveFile()
    {
        try
        {
            string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
            
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
                
                if (enableDebugLogs)
                {
                    Debug.Log("Save file deleted");
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to delete save file: {e.Message}");
        }
    }

    #endregion

    #region Scene Management

    public void TransitionToScene(string sceneName, Vector3? spawnPosition = null, string spawnId = null)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Cannot transition to scene - scene name is empty");
            return;
        }

        // Save current scene name for spawn point selection
        previousSceneName = SceneManager.GetActiveScene().name;

        // Save current player state before transition
        MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
        if (player != null)
        {
            SaveGameState(player);
        }

        // Set spawn position/ID for the new scene
        if (spawnPosition.HasValue)
        {
            currentGameState.playerPosition = spawnPosition.Value;
            currentGameState.useCustomSpawnPosition = true;
        }
        else
        {
            // Clear custom position to use spawn points
            currentGameState.useCustomSpawnPosition = false;
        }
        
        // Store spawn ID for later use
        if (!string.IsNullOrEmpty(spawnId))
        {
            currentGameState.targetSpawnId = spawnId;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Transitioning from {previousSceneName} to {sceneName}");
            if (spawnPosition.HasValue)
            {
                Debug.Log($"Using custom spawn position: {spawnPosition.Value}");
            }
            else if (!string.IsNullOrEmpty(spawnId))
            {
                Debug.Log($"Looking for spawn point with ID: {spawnId}");
            }
        }

        // Notify other scripts about scene transition
        OnSceneTransition?.Invoke(sceneName);

        // Load the new scene
        SceneManager.LoadScene(sceneName);
    }

    public void TransitionToSceneAsync(string sceneName, Vector3? spawnPosition = null, string spawnId = null)
    {
        StartCoroutine(TransitionToSceneCoroutine(sceneName, spawnPosition, spawnId));
    }

    private System.Collections.IEnumerator TransitionToSceneCoroutine(string sceneName, Vector3? spawnPosition = null, string spawnId = null)
    {
        // Save current scene name for spawn point selection
        previousSceneName = SceneManager.GetActiveScene().name;

        // Save current player state before transition
        MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
        if (player != null)
        {
            SaveGameState(player);
        }

        // Set spawn position/ID for the new scene
        if (spawnPosition.HasValue)
        {
            currentGameState.playerPosition = spawnPosition.Value;
            currentGameState.useCustomSpawnPosition = true;
        }
        else
        {
            currentGameState.useCustomSpawnPosition = false;
        }
        
        if (!string.IsNullOrEmpty(spawnId))
        {
            currentGameState.targetSpawnId = spawnId;
        }

        if (enableDebugLogs)
        {
            Debug.Log($"Transitioning from {previousSceneName} to {sceneName}");
        }

        // Notify other scripts about scene transition
        OnSceneTransition?.Invoke(sceneName);

        // Load the new scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    #endregion

    #region Public Getters

    public int GetCoins() => currentGameState.coins;
    public int GetLives() => currentGameState.lives;
    public float GetElapsedTime() => currentGameState.elapsedTime;
    public bool HasFirePower() => currentGameState.hasFirePower;
    public bool IsGrown() => currentGameState.isGrown;
    public string GetCurrentScene() => currentGameState.currentScene;
    public Vector3 GetPlayerPosition() => currentGameState.playerPosition;
    public string GetPreviousScene() => previousSceneName;

    #endregion

    #region Unity Events

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        currentGameState.currentScene = scene.name;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Scene loaded: {scene.name} (previous: {previousSceneName})");
        }

        // Auto-load game state for player in new scene
        StartCoroutine(LoadGameStateAfterSceneLoad());
    }

    private System.Collections.IEnumerator LoadGameStateAfterSceneLoad()
    {
        // Wait a frame for the scene to fully load
        yield return null;
        
        // Find player in new scene and load state
        MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
        if (player != null)
        {
            LoadGameState(player);
            
            // Handle positioning
            Vector3? targetPosition = null;
            
            if (currentGameState.useCustomSpawnPosition)
            {
                // Use explicit spawn position
                targetPosition = currentGameState.playerPosition;
                if (enableDebugLogs)
                {
                    Debug.Log($"Using custom spawn position: {targetPosition.Value}");
                }
            }
            else
            {
                // Try to find appropriate spawn point
                targetPosition = FindSpawnPosition(currentGameState.targetSpawnId);
                if (enableDebugLogs && targetPosition.HasValue)
                {
                    Debug.Log($"Using spawn point position: {targetPosition.Value}");
                }
            }
            
            // Apply the position
            if (targetPosition.HasValue)
            {
                player.SetPosition(targetPosition.Value);
            }
            
            // Clear the spawn settings after use
            currentGameState.useCustomSpawnPosition = false;
            currentGameState.targetSpawnId = "";
        }
    }

    #endregion

    #region Application Events

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && saveToDisk)
        {
            // Save when application is paused (mobile)
            MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
            if (player != null)
            {
                SaveGameState(player);
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && saveToDisk)
        {
            // Save when application loses focus
            MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
            if (player != null)
            {
                SaveGameState(player);
            }
        }
    }

    #endregion

    #region Context Menu Debug

    [ContextMenu("Log Current Game State")]
    public void DebugLogGameState()
    {
        currentGameState.LogState();
    }

    [ContextMenu("Reset Game State")]
    public void DebugResetGameState()
    {
        ResetGameState();
    }

    [ContextMenu("Save Current Player State")]
    public void DebugSavePlayerState()
    {
        MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
        if (player != null)
        {
            SaveGameState(player);
        }
        else
        {
            Debug.LogWarning("No player found to save state from");
        }
    }

    [ContextMenu("Save to Disk")]
    public void DebugSaveToDisk()
    {
        SaveGameStateToDisk();
    }

    [ContextMenu("Load from Disk")]
    public void DebugLoadFromDisk()
    {
        LoadGameStateFromDisk();
    }

    [ContextMenu("Delete Save File")]
    public void DebugDeleteSaveFile()
    {
        DeleteSaveFile();
    }

    [ContextMenu("Find Spawn Points")]
    public void DebugFindSpawnPoints()
    {
        SpawnPoint[] spawnPoints = FindObjectsOfType<SpawnPoint>();
        Debug.Log($"Found {spawnPoints.Length} spawn points in current scene:");
        foreach (SpawnPoint sp in spawnPoints)
        {
            Debug.Log($"- {sp.spawnPointId} at {sp.GetSpawnPosition()}");
        }
    }

    #endregion
}