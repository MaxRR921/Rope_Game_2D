using UnityEngine;

/// <summary>
/// Smoothly follows a target (e.g. the player) in a 2D scene without jitter.
/// Attach this script to your Main Camera GameObject and configure in the Inspector.
/// </summary>
public class CameraFollow2D : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("Transform of the object the camera will follow (usually the player)")]
    public Transform target;

    [Header("Follow Settings")]
    [Tooltip("Offset from the target's position; ensure Z = -10 for 2D")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("Approximate time (in seconds) for the camera to catch up to the target")]
    [Min(0.01f)]
    public float smoothTime = 0.2f;

    // Velocity reference for SmoothDamp
    private Vector3 currentVelocity = Vector3.zero;

    private void LateUpdate()
    {
        if (target == null)
            return;

        // Compute where the camera aims to be
        Vector3 desiredPosition = target.position + offset;
        // Smoothly move the camera without overshooting
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            current: transform.position,
            target: desiredPosition,
            currentVelocity: ref currentVelocity,
            smoothTime: smoothTime);

        transform.position = smoothedPosition;
    }
}

/*
Setup Instructions:
1. Select your Main Camera in the Scene.
2. Attach this CameraFollow2D.cs script.
3. In the Inspector:
   - Assign your Player's Transform to the "Target" field.
   - Set offset.z = -10 to maintain proper camera depth.
   - Adjust smoothTime (e.g., 0.1–0.3) for faster or slower catch-up.
4. Play the scene; the camera will smoothly follow without stuttering.
   4. Play the scene; the camera will smoothly follow your player.

   **Tips for reducing stutter:**
   - Ensure your Player's Rigidbody2D has **Interpolation** set to **Interpolate** in the Inspector so its Transform updates are smoothed between physics steps.
   - If jitter persists, consider using Unity's **Cinemachine** package and a **Cinemachine Virtual Camera** for advanced smoothing and dead-zone control.
*/

