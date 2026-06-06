using UnityEngine;
using System.Collections;

public class DelayedActivator : MonoBehaviour
{
    [Header("Target")]
    public GameObject targetObject;          // the GameObject to activate

    [Header("Timing")]
    public float delayBeforeShow = 2f;       // wait time before showing

    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target object not assigned!");
            enabled = false;
            return;
        }

        // Start with the object hidden (optional)
        // targetObject.SetActive(false);

        StartCoroutine(ShowAfterDelay());
    }

    IEnumerator ShowAfterDelay()
    {
        yield return new WaitForSeconds(delayBeforeShow);
        targetObject.SetActive(true);
    }
}