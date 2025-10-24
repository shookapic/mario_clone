using UnityEngine;

public class MyCamera : MonoBehaviour
{
    public Transform player;

    [Header("Follow Settings")]
    public float horizontalSmooth = 0.1f;
    public float verticalSmooth = 0.05f;

    [Header("Vertical Thresholds")]
    public float topMargin = 2f;
    public float bottomMargin = 2f;

    [Header("Camera Limits")]
    public Vector2 minLimits;
    public Vector2 maxLimits;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (player == null) return;

        Vector3 targetPos = transform.position;

        // --- Horizontal follow (always) ---
        targetPos.x = Mathf.Lerp(transform.position.x, player.position.x, horizontalSmooth);

        // --- Vertical follow (only if player goes above/below threshold) ---
        float deltaY = player.position.y - transform.position.y;

        if (deltaY > topMargin)
            targetPos.y = Mathf.Lerp(transform.position.y, player.position.y - topMargin, verticalSmooth);
        else if (deltaY < -bottomMargin)
            targetPos.y = Mathf.Lerp(transform.position.y, player.position.y + bottomMargin, verticalSmooth);

        // --- Clamp camera to level bounds ---
        targetPos.x = Mathf.Clamp(targetPos.x, minLimits.x, maxLimits.x);
        targetPos.y = Mathf.Clamp(targetPos.y, minLimits.y, maxLimits.y);
        targetPos.z = transform.position.z; // keep camera z

        transform.position = targetPos;
    }
}
