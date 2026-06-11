using UnityEngine;

public class MainMenuCollectibleHandler : MonoBehaviour
{
    [Header("Objects to Toggle")]
    public GameObject objectToDisable;
    public GameObject objectToEnable;

    void Start()
    {
        if (CollectibleManager.Instance != null)
            CollectibleManager.Instance.OnTargetReached += OnTargetReached;
    }

    private void OnTargetReached()
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