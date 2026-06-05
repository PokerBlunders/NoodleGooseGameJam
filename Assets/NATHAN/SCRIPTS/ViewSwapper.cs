using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ViewSwapper : MonoBehaviour
{
    public static ViewSwapper Instance { get; private set; }

    public enum ViewMode { Normal, Red, Blue }
    public ViewMode currentView = ViewMode.Normal;

    [Header("Post Process Volume")]
    public Volume postProcessVolume;
    private ColorAdjustments colorAdjustments;

    [Header("Tint Colors")]
    public Color redFilter = new Color(1f, 0.2f, 0.2f);
    public Color blueFilter = new Color(0.2f, 0.2f, 1f);

    [Header("Input")]
    public KeyCode swapKey = KeyCode.Q;   // keyboard fallback

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (postProcessVolume == null)
            postProcessVolume = FindObjectOfType<Volume>();

        if (postProcessVolume != null)
            postProcessVolume.profile.TryGet(out colorAdjustments);

        if (colorAdjustments == null)
            Debug.LogError("No Color Adjustments override found in the Volume!");

        ApplyView(currentView);
    }

    void Update()
    {
        // Keyboard fallback (still works if no voice)
        if (Input.GetKeyDown(swapKey))
            ToggleView();
    }

    // Called by voice command ("swap") or by other scripts
    public void ToggleView()
    {
        currentView = (ViewMode)(((int)currentView + 1) % 3);
        ApplyView(currentView);
    }

    void ApplyView(ViewMode mode)
    {
        if (colorAdjustments != null)
        {
            switch (mode)
            {
                case ViewMode.Red:
                    colorAdjustments.colorFilter.Override(redFilter);
                    break;
                case ViewMode.Blue:
                    colorAdjustments.colorFilter.Override(blueFilter);
                    break;
                default:
                    colorAdjustments.colorFilter.Override(Color.white);
                    break;
            }
        }

        // Hide/show objects based on tags
        GameObject[] redObjects = GameObject.FindGameObjectsWithTag("RedObject");
        GameObject[] blueObjects = GameObject.FindGameObjectsWithTag("BlueObject");

        bool showRed = (mode != ViewMode.Red);
        bool showBlue = (mode != ViewMode.Blue);

        foreach (GameObject obj in redObjects)
            SetRenderersAndColliders(obj, showRed);
        foreach (GameObject obj in blueObjects)
            SetRenderersAndColliders(obj, showBlue);
    }

    void SetRenderersAndColliders(GameObject obj, bool active)
    {
        foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            rend.enabled = active;
        foreach (Collider col in obj.GetComponentsInChildren<Collider>())
            col.enabled = active;
    }
}