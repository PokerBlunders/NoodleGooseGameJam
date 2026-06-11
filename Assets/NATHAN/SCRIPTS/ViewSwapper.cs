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
            Debug.Log("ColorAdjustments found – tint will work.");
        else
            Debug.LogError("ColorAdjustments not found!");

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

        // Find all Renderers and Colliders – this includes children automatically
        Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        foreach (Renderer rend in allRenderers)
        {
            if (rend == null) continue;
            int layer = rend.gameObject.layer;
            if (((1 << layer) & redLayer) != 0)
                rend.enabled = redVisible;
            else if (((1 << layer) & blueLayer) != 0)
                rend.enabled = blueVisible;
            // objects on other layers keep their existing enabled state
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

        OnViewChanged?.Invoke(mode);
    }
}