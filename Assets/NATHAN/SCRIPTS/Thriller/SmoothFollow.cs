using UnityEngine;

public class SmoothFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetA;
    public Transform targetB;

    [Header("Camera Settings for Target A")]
    public float distanceA = -1f;
    public float heightA = 0.1f;
    public float smoothA = 1f;

    [Header("Camera Settings for Target B")]
    public float distanceB = -3.5f;
    public float heightB = 0.5f;
    public float smoothB = 0.2f;

    [Header("Target Position Blend (smooth follow swap)")]
    public float targetBlendDelay = 5f;
    public float targetBlendDuration = 2f;
    private float targetBlend = 0f;       // 0 = A, 1 = B
    private float targetBlendStart = -1f;

    [Header("Camera Settings Blend (smooth offset change)")]
    public float settingsBlendDelay = 5f;
    public float settingsBlendDuration = 2f;
    private float settingsBlend = 0f;
    private float settingsBlendStart = -1f;

    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        if (targetA == null || targetB == null)
        {
            Debug.LogError("Assign both targets!");
            enabled = false;
            return;
        }
        Invoke(nameof(StartTargetBlend), targetBlendDelay);
        Invoke(nameof(StartSettingsBlend), settingsBlendDelay);
    }

    void StartTargetBlend()
    {
        targetBlendStart = Time.time;
        velocity = Vector3.zero; // reset to avoid a jerk
    }

    void StartSettingsBlend()
    {
        settingsBlendStart = Time.time;
    }

    void LateUpdate()
    {
        // Update target blend factor
        if (targetBlendStart >= 0f)
        {
            float elapsed = Time.time - targetBlendStart;
            if (elapsed >= targetBlendDuration)
            {
                targetBlend = 1f;
                targetBlendStart = -1f;
            }
            else
            {
                targetBlend = Mathf.Clamp01(elapsed / targetBlendDuration);
            }
        }

        // Update settings blend factor
        if (settingsBlendStart >= 0f)
        {
            float elapsed = Time.time - settingsBlendStart;
            if (elapsed >= settingsBlendDuration)
            {
                settingsBlend = 1f;
                settingsBlendStart = -1f;
            }
            else
            {
                settingsBlend = Mathf.Clamp01(elapsed / settingsBlendDuration);
            }
        }

        // Smoothly interpolate target position and forward direction
        Vector3 targetPos = Vector3.Lerp(targetA.position, targetB.position, targetBlend);
        Vector3 targetForward = Vector3.Slerp(targetA.forward, targetB.forward, targetBlend).normalized;

        // Smoothly interpolate camera settings
        float curDist = Mathf.Lerp(distanceA, distanceB, settingsBlend);
        float curHeight = Mathf.Lerp(heightA, heightB, settingsBlend);
        float curSmooth = Mathf.Lerp(smoothA, smoothB, settingsBlend);

        // Desired position
        Vector3 desiredPos = targetPos - targetForward * curDist + Vector3.up * curHeight;

        // Smooth move
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, curSmooth);
        transform.LookAt(targetPos);
    }
}