using UnityEngine;

public class BridgeSpawner : MonoBehaviour
{
    [Header("Sun")]
    public Transform sunLight;

    [Header("Bridge Prefab")]
    public GameObject bridgePrefab;

    [Header("Settings")]
    public float maxDistance = 30f;
    public LayerMask groundMask = -1;
    public float bridgeWidth = 1.5f;
    public float bridgeThickness = 0.2f;
    public float groundClearance = 0.05f;

    [Header("Bridge Start Position")]
    public Vector3 startOffset = Vector3.zero;

    [Header("Outline Settings")]
    public float outlineOffset = 0.05f;
    public string outlineChildName = "Outline";

    [Header("Shadow Obstruction")]
    public LayerMask obstructionMask = -1;   // objects that block sunlight or shadow
    public float sunCheckDistance = 500f;    // far distance to check for sunlight obstruction
    public bool showObstructionDebug = true;

    private CharacterController controller;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (sunLight == null)
        {
            Light sun = FindFirstObjectByType<Light>();
            if (sun != null && sun.type == LightType.Directional)
                sunLight = sun.transform;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
            TrySpawnBridge();
    }

    void TrySpawnBridge()
    {
        if (sunLight == null) return;

        Vector3 headTop = transform.position + Vector3.up * controller.height;
        Vector3 bridgeStart = transform.position + startOffset;
        Vector3 rayDir = sunLight.forward;           // direction from sun towards ground

        // --- CHECK 1: Is sunlight reaching the player's head? (raycast far behind player) ---
        Vector3 sunDirection = -rayDir; // from head towards the sun
        if (Physics.Raycast(headTop, sunDirection, out RaycastHit sunBlock, sunCheckDistance, obstructionMask))
        {
            if (showObstructionDebug)
            {
                Debug.Log($"Sunlight blocked by {sunBlock.collider.name} at distance {sunBlock.distance} – cannot place bridge.");
                Debug.DrawLine(headTop, sunBlock.point, Color.magenta, 1f);
            }
            return; // in shadow, no bridge
        }

        // --- CHECK 2: Does the shadow reach the ground without being blocked? ---
        if (Physics.Raycast(headTop, rayDir, out RaycastHit groundHit, maxDistance, groundMask))
        {
            Vector3 shadowTip = groundHit.point;
            float length = Vector3.Distance(bridgeStart, shadowTip);
            if (length < 0.5f) return;

            // Ray from head to shadow tip – check for obstructions (walls, etc.)
            Vector3 shadowDirection = (shadowTip - headTop).normalized;
            float shadowDistance = Vector3.Distance(headTop, shadowTip);
            if (Physics.Raycast(headTop, shadowDirection, out RaycastHit obstruction, shadowDistance, obstructionMask))
            {
                if (showObstructionDebug)
                {
                    Debug.Log($"Shadow blocked by {obstruction.collider.name} before reaching ground.");
                    Debug.DrawLine(headTop, obstruction.point, Color.red, 1f);
                }
                return;
            }

            // All clear – spawn bridge
            SpawnBridgeBetween(bridgeStart, shadowTip);
        }
    }

    void SpawnBridgeBetween(Vector3 start, Vector3 end)
    {
        if (bridgePrefab == null) return;

        GameObject bridge = Instantiate(bridgePrefab);
        bridge.name = "ShadowBridge";

        Vector3 center = (start + end) / 2f;
        float length = Vector3.Distance(start, end);
        Vector3 direction = (end - start).normalized;

        Quaternion rotation = Quaternion.LookRotation(direction);
        bridge.transform.position = center;
        bridge.transform.rotation = rotation;
        bridge.transform.localScale = new Vector3(bridgeWidth, bridgeThickness, length);

        // Outline child scaling
        Transform outline = bridge.transform.Find(outlineChildName);
        if (outline != null)
        {
            float offset = outlineOffset;
            outline.localScale = new Vector3(
                (bridgeWidth + offset * 2) / bridgeWidth,
                (bridgeThickness + offset * 2) / bridgeThickness,
                (length + offset * 2) / length
            );
        }

        // Lift above ground
        bridge.transform.position += Vector3.up * (bridgeThickness * 0.5f + groundClearance);

        // Setup physics for grab
        Rigidbody rb = bridge.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }
}