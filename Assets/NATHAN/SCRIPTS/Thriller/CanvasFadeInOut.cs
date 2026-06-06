using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class CanvasFadeInOut : MonoBehaviour
{
    [Header("Fade Settings")]
    public float fadeInDuration = 1f;     // time to fade from fully opaque to transparent
    public float delayBeforeFadeOut = 2f; // time to wait after fade in before starting fade out
    public float fadeOutDuration = 1f;    // time to fade back to opaque

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    void Start()
    {
        // Start fully opaque
        canvasGroup.alpha = 1f;
        StartCoroutine(FadeSequence());
    }

    IEnumerator FadeSequence()
    {
        // Fade in (opaque -> transparent)
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }
        canvasGroup.alpha = 0f;

        // Wait before fading out
        yield return new WaitForSeconds(delayBeforeFadeOut);

        // Fade out (transparent -> opaque)
        elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}