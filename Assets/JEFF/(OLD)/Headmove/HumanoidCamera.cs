using UnityEngine;

/// <summary>
/// Simulates human-like head movement for a camera: bobbing while moving + neck inertia when rotating.
/// Attach this to a camera that is a child of the player root (or follows the target).
/// </summary>
public class HumanoidCamera : MonoBehaviour
{
    [Header("Bobbing - Movement Oscillation")]
    [Tooltip("Vertical bounce amplitude (up/down displacement).")]
    public float verticalAmplitude = 0.05f;
    [Tooltip("Horizontal sway amplitude (left/right displacement).")]
    public float horizontalAmplitude = 0.03f;
    [Tooltip("Bounce frequency multiplier (linked to movement speed).")]
    public float bounceFrequency = 2.0f;
    [Tooltip("Distance covered in one full gait cycle.")]
    public float strideLength = 2.5f;

    [Header("Neck Inertia - Look Drag")]
    [Tooltip("Smooth time for rotation (higher = more inertia).")]
    public float rotationSmoothTime = 0.12f;
    [Tooltip("Max angular speed to prevent excessive whip.")]
    public float maxRotationSpeed = 300f;
    [Tooltip("Extra inertia multiplier while moving, making turning feel heavier.")]
    public float movingInertiaMultiplier = 1.5f;

    [Header("References")]
    [Tooltip("If assigned, uses this Rigidbody's velocity for bobbing; otherwise estimates from parent displacement.")]
    public Rigidbody targetRigidbody;
    [Tooltip("If using a CharacterController, assign it here to automatically get velocity.")]
    public CharacterController characterController;

    // Internal state
    private float currentYaw;
    private float currentPitch;
    private float targetYaw;
    private float targetPitch;
    private float yawVelocity, pitchVelocity;
    private Vector3 lastPosition;
    private float distanceTraveled;
    private Vector3 cameraLocalStartPos;

    void Start()
    {
        Vector3 angles = transform.localEulerAngles;
        currentYaw = targetYaw = angles.y;
        currentPitch = targetPitch = angles.x;

        cameraLocalStartPos = transform.localPosition;
        lastPosition = transform.position;
        distanceTraveled = 0f;
    }

    void Update()
    {
        // 1. Mouse input -> target rotation
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        targetYaw += mouseX;
        targetPitch -= mouseY;
        targetPitch = Mathf.Clamp(targetPitch, -80f, 80f);

        // 2. Get movement velocity
        Vector3 velocity = Vector3.zero;
        if (targetRigidbody != null)
            velocity = targetRigidbody.linearVelocity;
        else if (characterController != null)
            velocity = characterController.velocity;
        else
            velocity = (transform.position - lastPosition) / Time.deltaTime;

        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        lastPosition = transform.position;

        // 3. Calculate bobbing offsets (gait cycle based)
        float stepCycle = 0f;
        float verticalBounce = 0f;
        float horizontalSway = 0f;

        if (horizontalSpeed > 0.1f)
        {
            distanceTraveled += horizontalSpeed * Time.deltaTime;
            stepCycle = (distanceTraveled / strideLength) * Mathf.PI * 2f;

            // Vertical: sine wave (low when foot plants, high when lifting)
            verticalBounce = Mathf.Sin(stepCycle * bounceFrequency) * verticalAmplitude;
            // Horizontal: half-frequency cosine, simulating weight shift
            horizontalSway = Mathf.Cos(stepCycle * bounceFrequency * 0.5f) * horizontalAmplitude;
        }
        else
        {
            // Reset cycle when standing still
            distanceTraveled = 0f;
        }

        // 4. Apply neck inertia (smooth rotation damping)
        float smoothTime = rotationSmoothTime;
        if (horizontalSpeed > 0.5f)
            smoothTime *= movingInertiaMultiplier;

        currentYaw = Mathf.SmoothDampAngle(currentYaw, targetYaw, ref yawVelocity, smoothTime, maxRotationSpeed);
        currentPitch = Mathf.SmoothDampAngle(currentPitch, targetPitch, ref pitchVelocity, smoothTime, maxRotationSpeed);

        // 5. Apply to transform
        transform.localRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Local position: X = sway (left/right), Y = bounce (up/down), Z unchanged
        transform.localPosition = cameraLocalStartPos + new Vector3(horizontalSway, verticalBounce, 0f);
    }
}