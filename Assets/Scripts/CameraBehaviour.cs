using UnityEngine;

public class MyCamera : MonoBehaviour
{
    public Transform player;

    [Header("Follow Settings")]
    public float horizontalSmooth = 0.3f; // Increased for more responsive following
    
    [Header("Y-Axis Settings")]
    [Tooltip("Lock Y position to initial camera height")]
    public bool lockYPosition = true;
    [Tooltip("Follow player when they're about to go off-screen vertically")]
    public bool adaptiveYFollow = true;
    [Tooltip("Distance from screen edge before camera starts following Y (in world units)")]
    public float screenEdgeBuffer = 2f;
    [Tooltip("Speed of adaptive Y following")]
    public float adaptiveYSmooth = 0.5f;
    
    [Header("Legacy Y Settings (used when lockYPosition is false)")]
    [Tooltip("Manual override for Y position (only used if lockYPosition is false)")]
    public float verticalSmooth = 0.2f;
    [Tooltip("Vertical thresholds (only used if lockYPosition is false)")]
    public float topMargin = 1.5f;
    [Tooltip("Vertical thresholds (only used if lockYPosition is false)")]
    public float bottomMargin = 1.5f;

    [Header("Camera Limits")]
    public Vector2 minLimits = new Vector2(-1000, -1000); // Default to very large limits
    public Vector2 maxLimits = new Vector2(1000, 1000);   // Default to very large limits

    [Header("Zoom Settings")]
    [Tooltip("For Orthographic cameras: Controls the orthographic size (lower = more zoomed in)")]
    public float orthographicSize = 5f;
    [Tooltip("For Perspective cameras: Controls the field of view (lower = more zoomed in)")]
    public float fieldOfView = 60f;
    [Tooltip("Smooth zoom transitions")]
    public float zoomSmooth = 2f;

    private Vector3 velocity = Vector3.zero;
    private bool debugMode = true; // Set to false to disable debug logs
    private Camera cam;
    private float targetOrthographicSize;
    private float targetFieldOfView;
    private float initialYPosition; // Store the initial Y position
    private float currentLockedY; // Current Y position (can be different from initial if adaptive)

    void Start()
    {
        // Store the initial Y position to lock it
        initialYPosition = transform.position.y;
        currentLockedY = initialYPosition;
        Debug.Log($"MyCamera: Initial Y position locked at {initialYPosition}");

        // Get camera component
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("MyCamera: No Camera component found! This script must be attached to a GameObject with a Camera component.");
        }
        else
        {
            // Set initial zoom values
            if (cam.orthographic)
            {
                targetOrthographicSize = orthographicSize;
                cam.orthographicSize = orthographicSize;
                Debug.Log($"MyCamera: Orthographic camera detected, zoom size set to {orthographicSize}");
            }
            else
            {
                targetFieldOfView = fieldOfView;
                cam.fieldOfView = fieldOfView;
                Debug.Log($"MyCamera: Perspective camera detected, field of view set to {fieldOfView}");
            }
        }

        // Force reasonable camera limits if they're too restrictive
        if (minLimits.x >= 0 && minLimits.y >= 0 && maxLimits.x <= 10 && maxLimits.y <= 10)
        {
            Debug.LogWarning("MyCamera: Camera limits appear too restrictive, setting to reasonable defaults");
            minLimits = new Vector2(-1000, -1000);
            maxLimits = new Vector2(1000, 1000);
        }

        // Debug: Check if player is assigned
        if (player == null)
        {
            Debug.LogError("MyCamera: Player Transform is not assigned! Please assign the player Transform in the inspector.");

            // Try to find the player automatically
            GameObject playerObj = GameObject.FindWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
                Debug.Log("MyCamera: Found player automatically by tag 'Player'");
            }
            else
            {
                // Try to find by component
                MyPlayerMovement playerMovement = FindObjectOfType<MyPlayerMovement>();
                if (playerMovement != null)
                {
                    player = playerMovement.transform;
                    Debug.Log("MyCamera: Found player automatically by MyPlayerMovement component");
                }
            }
        }
        else
        {
            Debug.Log("MyCamera: Player Transform assigned successfully: " + player.name);
        }

        // Log initial positions for debugging
        if (debugMode)
        {
            Debug.Log($"MyCamera: Camera limits - Min: {minLimits}, Max: {maxLimits}");
            Debug.Log($"MyCamera: Initial camera position: {transform.position}");
            Debug.Log($"MyCamera: Y-axis locked: {lockYPosition}, Adaptive Y: {adaptiveYFollow}");
            if (player != null)
            {
                Debug.Log($"MyCamera: Initial player position: {player.position}");
                Debug.Log($"MyCamera: Initial distance: {Vector3.Distance(transform.position, player.position)}");
            }
        }
    }

    void LateUpdate()
    {
        if (player == null)
        {
            if (debugMode && Time.frameCount % 120 == 0) // Log every 2 seconds when player is null
            {
                Debug.LogWarning("MyCamera: Player Transform is null - camera cannot follow");
            }
            return;
        }

        Vector3 targetPos = transform.position;
        Vector3 originalPos = transform.position;

        // --- Horizontal follow (always) ---
        float targetX = Mathf.Lerp(transform.position.x, player.position.x, horizontalSmooth);
        targetPos.x = targetX;

        // --- Vertical follow logic ---
        if (lockYPosition)
        {
            if (adaptiveYFollow)
            {
                // Adaptive Y following - check if player is about to go off screen
                float screenTop = GetScreenBounds().y;
                float screenBottom = GetScreenBounds().w;
                
                bool playerNearTopEdge = player.position.y > (screenTop - screenEdgeBuffer);
                bool playerNearBottomEdge = player.position.y < (screenBottom + screenEdgeBuffer);
                
                if (playerNearTopEdge || playerNearBottomEdge)
                {
                    // Calculate target Y to keep player in view
                    float targetY;
                    if (playerNearTopEdge)
                    {
                        targetY = player.position.y - (GetCameraHeight() * 0.5f - screenEdgeBuffer);
                        if (debugMode) Debug.Log($"MyCamera: Player near top edge, adjusting camera Y up");
                    }
                    else
                    {
                        targetY = player.position.y + (GetCameraHeight() * 0.5f - screenEdgeBuffer);
                        if (debugMode) Debug.Log($"MyCamera: Player near bottom edge, adjusting camera Y down");
                    }
                    
                    // Smoothly move to new Y position
                    currentLockedY = Mathf.Lerp(currentLockedY, targetY, adaptiveYSmooth * Time.deltaTime);
                }
                // If player is back in safe zone, gradually return to original Y
                else if (Mathf.Abs(currentLockedY - initialYPosition) > 0.1f)
                {
                    currentLockedY = Mathf.Lerp(currentLockedY, initialYPosition, adaptiveYSmooth * 0.5f * Time.deltaTime);
                    if (debugMode && Time.frameCount % 60 == 0) Debug.Log($"MyCamera: Returning to initial Y position");
                }
                
                targetPos.y = currentLockedY;
            }
            else
            {
                // Simple Y lock to initial position
                targetPos.y = initialYPosition;
            }
        }
        else
        {
            // Original vertical following logic (only if player goes above/below threshold)
            float deltaY = player.position.y - transform.position.y;
            
            if (deltaY > topMargin)
            {
                float targetY = Mathf.Lerp(transform.position.y, player.position.y - topMargin, verticalSmooth);
                targetPos.y = targetY;
                if (debugMode) Debug.Log($"MyCamera: Following player UP - deltaY: {deltaY}");
            }
            else if (deltaY < -bottomMargin)
            {
                float targetY = Mathf.Lerp(transform.position.y, player.position.y + bottomMargin, verticalSmooth);
                targetPos.y = targetY;
                if (debugMode) Debug.Log($"MyCamera: Following player DOWN - deltaY: {deltaY}");
            }
        }

        // --- Clamp camera to level bounds ---
        float clampedX = Mathf.Clamp(targetPos.x, minLimits.x, maxLimits.x);
        float clampedY = Mathf.Clamp(targetPos.y, minLimits.y, maxLimits.y);
        
        // Check if clamping is affecting movement
        if (debugMode && (clampedX != targetPos.x || clampedY != targetPos.y))
        {
            Debug.LogError($"MyCamera: Camera movement clamped! Target: ({targetPos.x:F2}, {targetPos.y:F2}) -> Clamped: ({clampedX:F2}, {clampedY:F2})");
            Debug.LogError($"MyCamera: Current limits - Min: {minLimits}, Max: {maxLimits}");
        }
        
        targetPos.x = clampedX;
        targetPos.y = clampedY;
        targetPos.z = transform.position.z; // keep camera z

        // --- Handle zoom updates ---
        UpdateZoom();

        // Debug: Log movement information
        float moveDistance = Vector3.Distance(originalPos, targetPos);
        if (debugMode && moveDistance > 0.001f)
        {
            Debug.Log($"MyCamera: Moving {moveDistance:F3} units from {originalPos} to {targetPos} | Player at {player.position}");
        }
        else if (debugMode && Time.frameCount % 180 == 0) // Log every 3 seconds
        {
            float horizontalDistance = Mathf.Abs(player.position.x - transform.position.x);
            Vector4 bounds = GetScreenBounds();
            Debug.Log($"MyCamera: Status - Player: {player.position}, Camera: {transform.position}, HorizontalDist: {horizontalDistance:F2}, Y-Locked: {lockYPosition}, Adaptive: {adaptiveYFollow}");
            Debug.Log($"MyCamera: Screen bounds - Top: {bounds.y:F2}, Bottom: {bounds.w:F2}, Left: {bounds.x:F2}, Right: {bounds.z:F2}");
        }

        transform.position = targetPos;
    }

    // Helper method to get screen bounds in world coordinates
    Vector4 GetScreenBounds()
    {
        if (cam == null) return Vector4.zero;
        
        Vector3 bottomLeft = cam.ScreenToWorldPoint(new Vector3(0, 0, cam.nearClipPlane));
        Vector3 topRight = cam.ScreenToWorldPoint(new Vector3(cam.pixelWidth, cam.pixelHeight, cam.nearClipPlane));
        
        return new Vector4(bottomLeft.x, topRight.y, topRight.x, bottomLeft.y); // left, top, right, bottom
    }

    // Helper method to get camera height in world units
    float GetCameraHeight()
    {
        if (cam == null) return 10f;
        
        if (cam.orthographic)
        {
            return cam.orthographicSize * 2f;
        }
        else
        {
            float distance = Mathf.Abs(transform.position.z);
            return 2f * distance * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
    }

    void UpdateZoom()
    {
        if (cam == null) return;

        if (cam.orthographic)
        {
            // Update orthographic size targets from inspector
            targetOrthographicSize = orthographicSize;
            
            // Smooth zoom transition
            if (Mathf.Abs(cam.orthographicSize - targetOrthographicSize) > 0.001f)
            {
                cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetOrthographicSize, Time.deltaTime * zoomSmooth);
            }
        }
        else
        {
            // Update field of view targets from inspector
            targetFieldOfView = fieldOfView;
            
            // Smooth zoom transition
            if (Mathf.Abs(cam.fieldOfView - targetFieldOfView) > 0.001f)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFieldOfView, Time.deltaTime * zoomSmooth);
            }
        }
    }

    // Helper method you can call from inspector or other scripts to test
    [ContextMenu("Test Camera Follow")]
    public void TestCameraFollow()
    {
        if (player != null)
        {
            Vector4 bounds = GetScreenBounds();
            Debug.Log($"=== CAMERA TEST ===");
            Debug.Log($"Player position: {player.position}");
            Debug.Log($"Camera position: {transform.position}");
            Debug.Log($"Initial Y position: {initialYPosition}");
            Debug.Log($"Current locked Y: {currentLockedY}");
            Debug.Log($"Y-axis locked: {lockYPosition}, Adaptive Y: {adaptiveYFollow}");
            Debug.Log($"Screen bounds - Top: {bounds.y:F2}, Bottom: {bounds.w:F2}");
            Debug.Log($"Edge buffer: {screenEdgeBuffer}");
            Debug.Log($"Total distance: {Vector3.Distance(transform.position, player.position)}");
            Debug.Log($"Horizontal distance: {Mathf.Abs(player.position.x - transform.position.x)}");
            Debug.Log($"Vertical distance: {player.position.y - transform.position.y}");
            Debug.Log($"Camera limits: Min({minLimits}) Max({maxLimits})");
            if (cam != null)
            {
                if (cam.orthographic)
                    Debug.Log($"Orthographic size: {cam.orthographicSize}");
                else
                    Debug.Log($"Field of view: {cam.fieldOfView}");
            }
            Debug.Log($"===================");
        }
        else
        {
            Debug.LogError("Player Transform is not assigned!");
        }
    }

    // Reset Y position to current camera position
    [ContextMenu("Set Current Y as Lock Position")]
    public void SetCurrentYAsLockPosition()
    {
        initialYPosition = transform.position.y;
        currentLockedY = initialYPosition;
        Debug.Log($"MyCamera: Y lock position set to current position: {initialYPosition}");
    }

    // Force camera to move to a test position
    [ContextMenu("Force Camera Move Test")]
    public void ForceCameraMoveTest()
    {
        Vector3 testPos = transform.position + new Vector3(5, 0, 0); // Only move horizontally
        transform.position = testPos;
        Debug.Log($"MyCamera: Forced camera to position {testPos}");
    }

    // Fix camera limits to reasonable values
    [ContextMenu("Fix Camera Limits")]
    public void FixCameraLimits()
    {
        minLimits = new Vector2(-100, -50);
        maxLimits = new Vector2(100, 50);
        Debug.Log($"MyCamera: Fixed camera limits to Min: {minLimits}, Max: {maxLimits}");
    }

    // Set zoom to common presets
    [ContextMenu("Zoom: Close (3)")]
    public void SetZoomClose()
    {
        if (cam != null && cam.orthographic)
        {
            orthographicSize = 3f;
            Debug.Log("MyCamera: Set zoom to Close (orthographic size 3)");
        }
        else if (cam != null)
        {
            fieldOfView = 30f;
            Debug.Log("MyCamera: Set zoom to Close (field of view 30)");
        }
    }

    [ContextMenu("Zoom: Normal (5)")]
    public void SetZoomNormal()
    {
        if (cam != null && cam.orthographic)
        {
            orthographicSize = 5f;
            Debug.Log("MyCamera: Set zoom to Normal (orthographic size 5)");
        }
        else if (cam != null)
        {
            fieldOfView = 60f;
            Debug.Log("MyCamera: Set zoom to Normal (field of view 60)");
        }
    }

    [ContextMenu("Zoom: Far (8)")]
    public void SetZoomFar()
    {
        if (cam != null && cam.orthographic)
        {
            orthographicSize = 8f;
            Debug.Log("MyCamera: Set zoom to Far (orthographic size 8)");
        }
        else if (cam != null)
        {
            fieldOfView = 90f;
            Debug.Log("MyCamera: Set zoom to Far (field of view 90)");
        }
    }

    // Helper method to toggle debug mode
    [ContextMenu("Toggle Debug Mode")]
    public void ToggleDebugMode()
    {
        debugMode = !debugMode;
        Debug.Log($"MyCamera: Debug mode {(debugMode ? "enabled" : "disabled")}");
    }

    // Toggle Y position lock
    [ContextMenu("Toggle Y Position Lock")]
    public void ToggleYPositionLock()
    {
        lockYPosition = !lockYPosition;
        if (lockYPosition)
        {
            initialYPosition = transform.position.y; // Set current Y as the new lock position
            currentLockedY = initialYPosition;
        }
        Debug.Log($"MyCamera: Y position lock {(lockYPosition ? "enabled" : "disabled")} at Y={initialYPosition}");
    }

    // Toggle adaptive Y following
    [ContextMenu("Toggle Adaptive Y Follow")]
    public void ToggleAdaptiveYFollow()
    {
        adaptiveYFollow = !adaptiveYFollow;
        Debug.Log($"MyCamera: Adaptive Y follow {(adaptiveYFollow ? "enabled" : "disabled")}");
    }

    // Public methods for runtime zoom control
    public void SetZoom(float zoomValue)
    {
        if (cam != null && cam.orthographic)
        {
            orthographicSize = zoomValue;
        }
        else if (cam != null)
        {
            fieldOfView = zoomValue;
        }
    }

    public float GetCurrentZoom()
    {
        if (cam != null && cam.orthographic)
            return cam.orthographicSize;
        else if (cam != null)
            return cam.fieldOfView;
        return 0f;
    }

    // Public method to manually set Y lock position
    public void SetYLockPosition(float yPosition)
    {
        initialYPosition = yPosition;
        currentLockedY = yPosition;
        if (lockYPosition)
        {
            Vector3 pos = transform.position;
            pos.y = initialYPosition;
            transform.position = pos;
        }
        Debug.Log($"MyCamera: Y lock position set to {yPosition}");
    }
}
