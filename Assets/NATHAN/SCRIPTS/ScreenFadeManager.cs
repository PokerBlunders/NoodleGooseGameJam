using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScreenFadeManager : MonoBehaviour
{
    public static ScreenFadeManager Instance { get; private set; }

    [Header("Black Overlay (by name)")]
    public string blackOverlayName = "BlackFade";   // Name of the GameObject with CanvasGroup
    private CanvasGroup blackCanvasGroup;

    [Header("Black Overlay Fade Durations")]
    public float fadeInDuration = 0.5f;
    public float fadeOutDuration = 0.5f;
    public float fadeRespawnDelay = 0.2f;

    [Header("Auto Fade In (separate CanvasGroup)")]
    public string autoFadeOverlayName = "AutoFade";
    private CanvasGroup autoFadeCanvasGroup;
    public float autoFadeDelay = 2f;
    public float autoFadeDuration = 1f;
    public bool startFullyTransparent = true;

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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find black overlay by name
        GameObject blackObj = GameObject.Find(blackOverlayName);
        if (blackObj != null)
            blackCanvasGroup = blackObj.GetComponent<CanvasGroup>();
        else
            Debug.LogWarning($".");

        // Find auto‑fade overlay
        GameObject autoObj = GameObject.Find(autoFadeOverlayName);
        if (autoObj != null)
            autoFadeCanvasGroup = autoObj.GetComponent<CanvasGroup>();

        // Fade in black overlay
        if (blackCanvasGroup != null)
        {
            StopAllCoroutines();
            isFading = false;
            blackCanvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }

        // Auto‑fade routine for secondary overlay
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
        yield return new WaitForSeconds(autoFadeDelay);
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
            if (blackCanvasGroup != null)
                blackCanvasGroup.alpha = 1f - t;
            yield return null;
        }
        if (blackCanvasGroup != null)
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
            if (blackCanvasGroup != null)
                blackCanvasGroup.alpha = t;
            yield return null;
        }
        if (blackCanvasGroup != null)
            blackCanvasGroup.alpha = 1f;
        isFading = false;
    }

    public IEnumerator FadeOutAndIn(System.Action onComplete = null)
    {
        yield return StartCoroutine(FadeOut());
        yield return new WaitForSeconds(fadeRespawnDelay);
        onComplete?.Invoke();
        yield return StartCoroutine(FadeIn());
    }
}