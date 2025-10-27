using UnityEngine;
using System.Collections.Generic;

public class CheckpointManager : MonoBehaviour
{
    [Header("Checkpoint Management")]
    public bool debugMode = false;
    
    public static CheckpointManager Instance { get; private set; }
    
    // List of all checkpoints in the scene
    private List<FlagPole> checkpoints = new List<FlagPole>();
    private FlagPole currentActiveCheckpoint = null;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        RefreshCheckpointList();
    }
    
    private void Start()
    {
        // Additional setup if needed
        if (debugMode)
        {
            Debug.Log($"CheckpointManager initialized with {checkpoints.Count} checkpoints");
        }
    }
    
    public void RefreshCheckpointList()
    {
        checkpoints.Clear();
        FlagPole[] foundCheckpoints = FindObjectsOfType<FlagPole>();
        
        foreach (FlagPole checkpoint in foundCheckpoints)
        {
            if (!checkpoint.isLevelEndFlag) // Only add actual checkpoints, not level-end flags
            {
                checkpoints.Add(checkpoint);
            }
        }
        
        if (debugMode)
        {
            Debug.Log($"Refreshed checkpoint list: {checkpoints.Count} checkpoints found");
        }
    }
    
    public void RegisterActiveCheckpoint(FlagPole checkpoint)
    {
        if (checkpoint != null && !checkpoint.isLevelEndFlag)
        {
            currentActiveCheckpoint = checkpoint;
            
            if (debugMode)
            {
                Debug.Log($"Active checkpoint registered: {checkpoint.name}");
            }
        }
    }

    public FlagPole GetActiveCheckpoint()
    {
        // Verify the checkpoint is still valid and active
        if (currentActiveCheckpoint != null && currentActiveCheckpoint.IsCheckpointActive())
        {
            return currentActiveCheckpoint;
        }
        
        // If stored checkpoint is invalid, search for any active checkpoint
        foreach (FlagPole checkpoint in checkpoints)
        {
            if (checkpoint != null && checkpoint.ShouldUseForRespawn())
            {
                currentActiveCheckpoint = checkpoint;
                return checkpoint;
            }
        }
        
        return null;
    }
    
    public bool RespawnPlayerAtActiveCheckpoint()
    {
        FlagPole activeCheckpoint = GetActiveCheckpoint();
        
        if (activeCheckpoint != null)
        {
            activeCheckpoint.RespawnPlayerAtCheckpoint();
            
            if (debugMode)
            {
                Debug.Log($"Player respawned at checkpoint: {activeCheckpoint.name}");
            }
            
            return true;
        }
        
        if (debugMode)
        {
            Debug.LogWarning("No active checkpoint found for respawn");
        }
        
        return false;
    }
    

    public void ResetAllCheckpoints()
    {
        foreach (FlagPole checkpoint in checkpoints)
        {
            //checkpoint.ResetCheckpoint();
        }

        currentActiveCheckpoint = null;
        
        if (debugMode)
        {
            Debug.Log("All checkpoints reset");
        }
    }
    
    public void DebugCheckpointInfo()
    {
        Debug.Log($"=== Checkpoint Manager Debug ===");
        Debug.Log($"Total checkpoints: {checkpoints.Count}");
        Debug.Log($"Active checkpoint: {(currentActiveCheckpoint != null ? currentActiveCheckpoint.name : "None")}");
        
        for (int i = 0; i < checkpoints.Count; i++)
        {
            FlagPole checkpoint = checkpoints[i];
            if (checkpoint != null)
            {
                Debug.Log($"Checkpoint {i}: {checkpoint.name} - Active: {checkpoint.IsCheckpointActive()}");
            }
        }
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

  // Made for editor context menu debugging with AI  
    #if UNITY_EDITOR
    [ContextMenu("Debug Checkpoint Info")]
    public void DebugMenuCheckpointInfo()
    {
        DebugCheckpointInfo();
    }
    
    [ContextMenu("Refresh Checkpoints")]
    public void DebugRefreshCheckpoints()
    {
        RefreshCheckpointList();
    }
    #endif
}