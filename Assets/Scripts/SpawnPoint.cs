using UnityEngine;


public class SpawnPoint : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("The exact position where the player will spawn")]
    public Vector3 spawnPosition;
    
    [Tooltip("Use this GameObject's transform position as spawn point")]
    public bool useTransformPosition = true;
    
    [Header("Spawn Conditions")]
    [Tooltip("Only use this spawn point when coming from specific scene (leave empty for any scene)")]
    public string fromSceneName = "";
    
    [Tooltip("Unique identifier for this spawn point (useful for multiple spawn points in one scene)")]
    public string spawnPointId = "default";
    
    [Header("Visual Debug")]
    [Tooltip("Show spawn point in scene view")]
    public bool showInSceneView = true;
    
    [Tooltip("Color of the spawn point gizmo")]
    public Color gizmoColor = Color.green;

    void Start()
    {
        // Update spawn position to current transform position if needed
        if (useTransformPosition)
        {
            spawnPosition = transform.position;
        }
    }

    public Vector3 GetSpawnPosition()
    {
        return useTransformPosition ? transform.position : spawnPosition;
    }


    public bool ShouldUseForScene(string sourceScene)
    {
        return string.IsNullOrEmpty(fromSceneName) || fromSceneName.Equals(sourceScene, System.StringComparison.OrdinalIgnoreCase);
    }


    public bool MatchesId(string id)
    {
        return spawnPointId.Equals(id, System.StringComparison.OrdinalIgnoreCase);
    }

    void OnDrawGizmos()
    {
        if (!showInSceneView) return;

        Vector3 pos = GetSpawnPosition();
        
        // Draw spawn point marker
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
        Gizmos.DrawSphere(pos, 0.2f);
        
        // Draw arrow pointing up
        Gizmos.color = Color.white;
        Vector3 arrowStart = pos + Vector3.down * 0.3f;
        Vector3 arrowEnd = pos + Vector3.up * 0.3f;
        Gizmos.DrawLine(arrowStart, arrowEnd);
        
        // Arrow head
        Vector3 arrowHead1 = arrowEnd + (Vector3.left + Vector3.down) * 0.1f;
        Vector3 arrowHead2 = arrowEnd + (Vector3.right + Vector3.down) * 0.1f;
        Gizmos.DrawLine(arrowEnd, arrowHead1);
        Gizmos.DrawLine(arrowEnd, arrowHead2);

        // Draw label
        #if UNITY_EDITOR
        UnityEditor.Handles.Label(pos + Vector3.up * 0.5f, $"Spawn: {spawnPointId}");
        #endif
    }

    void OnDrawGizmosSelected()
    {
        if (!showInSceneView) return;

        Vector3 pos = GetSpawnPosition();
        
        // Draw larger highlight when selected
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pos, Vector3.one * 1.0f);
    }

    [ContextMenu("Test Spawn Position")]
    public void TestSpawnPosition()
    {
        Debug.Log($"SpawnPoint '{spawnPointId}' at position: {GetSpawnPosition()}");
        
        // Try to move player to this position if in play mode
        if (Application.isPlaying)
        {
            MyPlayerMovement player = FindObjectOfType<MyPlayerMovement>();
            if (player != null)
            {
                player.SetPosition(GetSpawnPosition());
                Debug.Log("Player moved to spawn position for testing");
            }
            else
            {
                Debug.LogWarning("No MyPlayerMovement found in scene");
            }
        }
    }
}