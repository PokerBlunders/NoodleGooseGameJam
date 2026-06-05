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

    [Header("Input")]
    public KeyCode swapKey = KeyCode.Q;

    public System.Action<ViewMode> OnViewChanged;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Find the volume if not assigned
        if (postProcessVolume == null)
            postProcessVolume = FindFirstObjectByType<Volume>();

        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out colorAdjustments))
        {
            Debug.Log("ColorAdjustments found – tint will work.");
        }
        else
        {
            Debug.LogError("ColorAdjustments not found! Please assign a Volume with Color Adjustments override.");
        }

        ApplyView(currentView);
    }

    void Update()
    {
        if (Input.GetKeyDown(swapKey))
            ToggleView();
    }

    public void ToggleView()
    {
        Debug.Log("ToggleView called, current view = " + currentView);
        currentView = (currentView == ViewMode.Blue) ? ViewMode.Red : ViewMode.Blue;
        ApplyView(currentView);
    }

    void ApplyView(ViewMode mode)
    {
        Debug.Log("ApplyView: " + mode);

        // Update colour filter
        if (colorAdjustments != null)
        {
            if (mode == ViewMode.Blue)
                colorAdjustments.colorFilter.Override(blueFilter);
            else
                colorAdjustments.colorFilter.Override(redFilter);
            Debug.Log("Color filter set to " + (mode == ViewMode.Blue ? "Blue" : "Red"));
        }
        else
        {
            Debug.LogWarning("ColorAdjustments missing – tint unchanged.");
        }

        // Hide/show objects based on tags
        GameObject[] redObjects = GameObject.FindGameObjectsWithTag("RedObject");
        GameObject[] blueObjects = GameObject.FindGameObjectsWithTag("BlueObject");

        bool showRed = (mode == ViewMode.Red);
        bool showBlue = (mode == ViewMode.Blue);

        foreach (GameObject obj in redObjects)
            SetRenderersAndColliders(obj, showRed);
        foreach (GameObject obj in blueObjects)
            SetRenderersAndColliders(obj, showBlue);

        // Notify listeners (for animation and blendshapes)
        OnViewChanged?.Invoke(mode);
    }

    void SetRenderersAndColliders(GameObject obj, bool active)
    {
        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            rend.enabled = active;
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = active;
    }
}