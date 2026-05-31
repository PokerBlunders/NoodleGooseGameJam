using UnityEngine;

/// <summary>
/// Manages player stealth state: moving = visible but makes sound, stationary = invisible but can only navigate by sound.
/// </summary>
public class PlayerStealthState : MonoBehaviour
{
    [Header("Stealth States")]
    [Tooltip("How fast player must move to be considered 'moving'.")]
    public float movementThreshold = 0.5f;
    [Tooltip("Sound radius when moving (enemies within this range will hear player).")]
    public float soundRadius = 8f;
    [Tooltip("How long after stopping before full invisibility kicks in.")]
    public float invisibilityTransitionTime = 0.3f;

    [Header("Visual Feedback")]
    [Tooltip("Renderer to fade when invisible (set to player body mesh).")]
    public Renderer[] playerRenderers;
    [Tooltip("Material opacity when fully invisible (0 = fully transparent, 1 = fully visible).")]
    [Range(0f, 1f)]
    public float invisibleAlpha = 0.15f;
    [Tooltip("How fast the visual fade happens.")]
    public float fadeSpeed = 5f;

    // Public state for enemies to read
    [HideInInspector]
    public bool isMoving;
    [HideInInspector]
    public bool isFullyInvisible;
    [HideInInspector]
    public float currentSoundRadius; // 0 when stationary, soundRadius when moving

    private CharacterController controller;
    private float currentAlpha = 1f;
    private float stationaryTime;
    private Vector3 lastPosition;
    private Material[] materials;
    private Color[] originalColors;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        lastPosition = transform.position;

        // Cache materials
        if (playerRenderers.Length > 0)
        {
            materials = new Material[playerRenderers.Length];
            originalColors = new Color[playerRenderers.Length];
            for (int i = 0; i < playerRenderers.Length; i++)
            {
                materials[i] = playerRenderers[i].material;
                originalColors[i] = materials[i].color;
            }
        }
    }

    void Update()
    {
        // Calculate horizontal speed
        Vector3 velocity = (transform.position - lastPosition) / Time.deltaTime;
        float horizontalSpeed = new Vector3(velocity.x, 0f, velocity.z).magnitude;
        lastPosition = transform.position;

        // Determine state
        isMoving = horizontalSpeed > movementThreshold;

        // Update stationary timer
        if (!isMoving)
        {
            stationaryTime += Time.deltaTime;
            isFullyInvisible = stationaryTime >= invisibilityTransitionTime;
        }
        else
        {
            stationaryTime = 0f;
            isFullyInvisible = false;
        }

        // Set sound radius
        currentSoundRadius = isMoving ? soundRadius : 0f;

        // Visual fading
        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        float targetAlpha = isFullyInvisible ? invisibleAlpha : 1f;
        currentAlpha = Mathf.MoveTowards(currentAlpha, targetAlpha, fadeSpeed * Time.deltaTime);

        for (int i = 0; i < materials.Length; i++)
        {
            Color c = originalColors[i];
            c.a = currentAlpha;
            materials[i].color = c;

            // Set rendering mode for transparency
            if (currentAlpha < 0.99f)
            {
                SetMaterialTransparent(materials[i]);
            }
            else
            {
                SetMaterialOpaque(materials[i]);
            }
        }
    }

    void SetMaterialTransparent(Material mat)
    {
        mat.SetFloat("_Surface", 1); // Transparent for URP/HDRP
        mat.SetFloat("_Blend", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
    }

    void SetMaterialOpaque(Material mat)
    {
        mat.SetFloat("_Surface", 0);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
        mat.SetInt("_ZWrite", 1);
        mat.renderQueue = -1;
    }

    // Visualize sound radius in editor
    void OnDrawGizmosSelected()
    {
        if (Application.isPlaying && isMoving)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, soundRadius);
        }
    }
}