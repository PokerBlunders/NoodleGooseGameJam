using UnityEngine;

public class SunController : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float sensitivity = 0.5f;
    public bool invertY = false;
    public bool clampVertical = true;
    public float minVerticalAngle = 10f;   // degrees above horizon
    public float maxVerticalAngle = 80f;   // degrees below zenith

    [Header("Optional Center Point")]
    public Transform pivotPoint;   // leave empty to rotate around world origin (0,0,0)
    public bool rotateAroundPlayer = false;

    // Public flag for other scripts (like PlayerController) to know we're using the mouse
    public static bool IsControllingSun { get; private set; }

    private float currentAngleX = 45f;
    private float currentAngleY = 45f;

    void Start()
    {
        Vector3 euler = transform.eulerAngles;
        currentAngleX = euler.y;
        currentAngleY = euler.x;
        if (currentAngleY > 180) currentAngleY -= 360;

        if (rotateAroundPlayer && pivotPoint == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) pivotPoint = player.transform;
        }
    }

    void Update()
    {
        // Right mouse button to control sun
        if (Input.GetMouseButton(1))
        {
            IsControllingSun = true;

            float dx = Input.GetAxis("Mouse X") * sensitivity;
            float dy = Input.GetAxis("Mouse Y") * sensitivity;

            if (invertY) dy = -dy;

            currentAngleX += dx;
            currentAngleY -= dy;

            if (clampVertical)
            {
                currentAngleY = Mathf.Clamp(currentAngleY, minVerticalAngle, maxVerticalAngle);
            }

            Quaternion rotation = Quaternion.Euler(currentAngleY, currentAngleX, 0f);
            transform.rotation = rotation;
        }
        else
        {
            IsControllingSun = false;
        }
    }

    public void ResetSun()
    {
        currentAngleX = 45f;
        currentAngleY = 45f;
        if (clampVertical) currentAngleY = Mathf.Clamp(currentAngleY, minVerticalAngle, maxVerticalAngle);
        transform.rotation = Quaternion.Euler(currentAngleY, currentAngleX, 0f);
    }

    public float GetHorizontalAngle() { return currentAngleX; }
    public float GetVerticalAngle() { return currentAngleY; }
}