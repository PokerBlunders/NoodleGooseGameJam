using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public class ViewSwapper : MonoBehaviour
{
    public static ViewSwapper Instance { get; private set; }

    public enum ViewMode { Blue, Red }
    private ViewMode currentView = ViewMode.Blue;

    [Header("Post Process Volume")]
    public Volume postProcessVolume;
    private ColorAdjustments colorAdjustments;

    [Header("Tint Colors")]
    public Color redFilter = new Color(1f, 0.2f, 0.2f);
    public Color blueFilter = new Color(0.2f, 0.2f, 1f);

    [Header("Input")]
    public KeyCode swapKey = KeyCode.Q;

    [Header("Layer Visibility")]
    public LayerMask redLayer;   // objects on this layer will be visible only in Red view
    public LayerMask blueLayer;  // objects on this layer will be visible only in Blue view

    // Cache to avoid re‑searching every frame (optional but efficient)
    private List<Renderer> allRenderers = new List<Renderer>();
    private List<Collider> allColliders = new List<Collider>();

    public System.Action<ViewMode> OnViewChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Cache all renderers and colliders once at start (assumes they don't change dynamically)
        allRenderers.Clear();
        allColliders.Clear();
        allRenderers.AddRange(FindObjectsByType<Renderer>(FindObjectsSortMode.None));
        allColliders.AddRange(FindObjectsByType<Collider>(FindObjectsSortMode.None));

        if (postProcessVolume == null)
            postProcessVolume = FindFirstObjectByType<Volume>();

        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out colorAdjustments))
            Debug.Log("ColorAdjustments found – tint will work.");
        else
            Debug.LogError("ColorAdjustments not found! Please assign a Volume with Color Adjustments override.");

        ApplyView(currentView);
    }

    void Update()
    {
        if (Input.GetKeyDown(swapKey))
            ToggleView();
    }

    public void ToggleView()
    {
        currentView = (currentView == ViewMode.Blue) ? ViewMode.Red : ViewMode.Blue;
        ApplyView(currentView);
    }

    void ApplyView(ViewMode mode)
    {
        // Update colour filter
        if (colorAdjustments != null)
        {
            if (mode == ViewMode.Blue)
                colorAdjustments.colorFilter.Override(blueFilter);
            else
                colorAdjustments.colorFilter.Override(redFilter);
        }

        // Determine which layers should be visible
        bool redVisible = (mode == ViewMode.Red);
        bool blueVisible = (mode == ViewMode.Blue);

        // Toggle renderers based on layer
        foreach (Renderer rend in allRenderers)
        {
            int objLayer = rend.gameObject.layer;
            if (((1 << objLayer) & redLayer) != 0)      // object is on red layer
                rend.enabled = redVisible;
            else if (((1 << objLayer) & blueLayer) != 0) // object is on blue layer
                rend.enabled = blueVisible;
            // objects on other layers remain always visible
        }

        // Toggle colliders similarly (so you can't collide with invisible objects)
        foreach (Collider col in allColliders)
        {
            int objLayer = col.gameObject.layer;
            if (((1 << objLayer) & redLayer) != 0)
                col.enabled = redVisible;
            else if (((1 << objLayer) & blueLayer) != 0)
                col.enabled = blueVisible;
        }

        OnViewChanged?.Invoke(mode);
    }
}