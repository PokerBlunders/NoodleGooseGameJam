using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

    [Header("Layer Visibility")]
    public LayerMask redLayer;
    public LayerMask blueLayer;

    [Header("Tag Visibility (colliders only – children included)")]
    public string blueTag = "BlueTop";
    public string redTag = "RedTop";

    public System.Action<ViewMode> OnViewChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (postProcessVolume == null)
            postProcessVolume = FindFirstObjectByType<Volume>();

        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out colorAdjustments))
            Debug.Log(".");
        else
            Debug.LogError(".");

        ApplyView(currentView);
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

        bool redVisible = (mode == ViewMode.Red);
        bool blueVisible = (mode == ViewMode.Blue);

        // --- 1. Renderers and Colliders by LAYER (original behaviour) ---
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;
            int layer = rend.gameObject.layer;
            if (((1 << layer) & redLayer) != 0)
                rend.enabled = redVisible;
            else if (((1 << layer) & blueLayer) != 0)
                rend.enabled = blueVisible;
        }

        Collider[] allColliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);
        foreach (Collider col in allColliders)
        {
            if (col == null) continue;
            int layer = col.gameObject.layer;
            if (((1 << layer) & redLayer) != 0)
                col.enabled = redVisible;
            else if (((1 << layer) & blueLayer) != 0)
                col.enabled = blueVisible;
        }

        // --- 2. EXTRA: Toggle COLLIDERS ONLY for tagged objects (including children) ---
        // BlueTag objects: colliders enabled only in Blue view
        GameObject[] blueTagged = GameObject.FindGameObjectsWithTag(blueTag);
        foreach (GameObject obj in blueTagged)
            ToggleCollidersOnly(obj, blueVisible);

        // RedTag objects: colliders enabled only in Red view
        GameObject[] redTagged = GameObject.FindGameObjectsWithTag(redTag);
        foreach (GameObject obj in redTagged)
            ToggleCollidersOnly(obj, redVisible);

        OnViewChanged?.Invoke(mode);
    }

    private void ToggleCollidersOnly(GameObject obj, bool active)
    {
        if (obj == null) return;
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = active;
    }
}