using UnityEngine;

public class MainMenuCollectibleHandler : MonoBehaviour
{
    [Header("Objects to Toggle")]
    public GameObject objectToDisable;
    public GameObject objectToEnable;

    void Start()
    {
        if (CollectibleManager.Instance != null)
        {
            // First, check if the target is already reached
            if (CollectibleManager.Instance.TotalCollected >= CollectibleManager.Instance.targetCollectibles)
            {
                Debug.Log("Target already reached – toggling objects immediately.");
                ToggleObjects();
            }
            else
            {
                // Subscribe to the event for when it becomes reached later
                CollectibleManager.Instance.OnTargetReached += OnTargetReached;
            }
        }
    }

    private void OnTargetReached()
    {
        Debug.Log("Target reached event fired – toggling objects.");
        ToggleObjects();
    }

    private void ToggleObjects()
    {
        if (objectToDisable != null)
            objectToDisable.SetActive(false);
        if (objectToEnable != null)
            objectToEnable.SetActive(true);
    }

    void OnDestroy()
    {
        if (CollectibleManager.Instance != null)
            CollectibleManager.Instance.OnTargetReached -= OnTargetReached;
    }
}