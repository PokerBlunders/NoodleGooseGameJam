using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFadeManager : MonoBehaviour
{
    public static ScreenFadeManager Instance { get; private set; }

    [Header("Black Overlay (for death / scene transitions)")]
    public CanvasGroup blackCanvasGroup;   // full‑screen black UI Image with CanvasGroup

    [Header("Black Overlay Fade Durations")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float fadeRespawnDelay = 0.2f;

    [Header("Auto Fade In (separate CanvasGroup)")]
    public CanvasGroup autoFadeCanvasGroup;   // CanvasGroup that fades in automatically after a delay
    public float autoFadeDelay = 2f;          // time to wait before starting fade in
    public float autoFadeDuration = 1f;       // how long the fade takes
    public bool startFullyTransparent = true; // start with alpha 0

    private bool isFading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // --- Black overlay initialisation (fade in from black) ---
        if (blackCanvasGroup != null)
        {
            blackCanvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }

        // --- Auto fade in CanvasGroup setup ---
        if (autoFadeCanvasGroup != null)
        {
            if (startFullyTransparent)
                autoFadeCanvasGroup.alpha = 0f;
            else
                autoFadeCanvasGroup.alpha = 1f;
            StartCoroutine(AutoFadeInRoutine());
        }
    }

    private IEnumerator AutoFadeInRoutine()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(autoFadeDelay);

        // Fade in the target CanvasGroup
        float elapsed = 0f;
        while (elapsed < autoFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / autoFadeDuration);
            if (autoFadeCanvasGroup != null)
                autoFadeCanvasGroup.alpha = t;
            yield return null;
        }
        if (autoFadeCanvasGroup != null)
            autoFadeCanvasGroup.alpha = 1f;
    }

    public IEnumerator FadeIn()
    {
        if (isFading) yield break;
        isFading = true;
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            blackCanvasGroup.alpha = 1f - t;
            yield return null;
        }
        blackCanvasGroup.alpha = 0f;
        isFading = false;
    }

    public IEnumerator FadeOut()
    {
        if (isFading) yield break;
        isFading = true;
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            blackCanvasGroup.alpha = t;
            yield return null;
        }
        blackCanvasGroup.alpha = 1f;
        isFading = false;
    }

    // Performs fade out → calls onComplete → then fade in (only for black overlay)
    public IEnumerator FadeOutAndIn(System.Action onComplete = null)
    {
        yield return StartCoroutine(FadeOut());
        yield return new WaitForSeconds(fadeRespawnDelay);
        onComplete?.Invoke();
        yield return StartCoroutine(FadeIn());
    }
}