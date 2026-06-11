using System.Collections;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.SceneManagement;

public class VoiceMenuLoader : MonoBehaviour
{
    [Header("Fade Settings (for returning to menu)")]
    public CanvasGroup blackFadeCanvasGroup;   // Full‑screen black CanvasGroup
    public float fadeDuration = 0.5f;          // How long the fade to black takes
    public float sceneLoadDelay = 0.2f;        // Extra delay after fade before loading

    [Header("Fade In After Delay (auto)")]
    public CanvasGroup autoFadeInCanvasGroup;  // CanvasGroup to fade in automatically
    public float autoFadeInDelay = 2f;         // Time to wait before starting fade in
    public float autoFadeInDuration = 1f;      // How long the fade in takes
    public bool startFullyTransparent = true;  // Whether to set alpha to 0 at start

    [Header("Scene")]
    public string menuSceneName = "MainMenu";

    private KeywordRecognizer keywordRecognizer;
    private bool isProcessing = false;

    void Start()
    {
        // --- Setup black fade (for returning to menu) ---
        if (blackFadeCanvasGroup == null)
        {
            enabled = false;
            return;
        }
        blackFadeCanvasGroup.alpha = 0f;

        // --- Setup auto fade in canvas group ---
        if (autoFadeInCanvasGroup != null)
        {
            if (startFullyTransparent)
                autoFadeInCanvasGroup.alpha = 0f;
            else
                autoFadeInCanvasGroup.alpha = 1f;
            StartCoroutine(AutoFadeInRoutine());
        }

        // Set up keyword recognizer for "menu"
        string[] keywords = { "menu" };
        keywordRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Medium);
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();
    }

    private IEnumerator AutoFadeInRoutine()
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(autoFadeInDelay);

        // Fade in the target canvas group
        float elapsed = 0f;
        while (elapsed < autoFadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / autoFadeInDuration);
            if (autoFadeInCanvasGroup != null)
                autoFadeInCanvasGroup.alpha = t;
            yield return null;
        }
        if (autoFadeInCanvasGroup != null)
            autoFadeInCanvasGroup.alpha = 1f;
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string word = args.text.ToLower();
        if (word == "menu" && !isProcessing)
        {
            isProcessing = true;
            StartCoroutine(FadeAndLoadMenu());
        }
    }

    private IEnumerator FadeAndLoadMenu()
    {
        // Fade to black
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (blackFadeCanvasGroup != null)
                blackFadeCanvasGroup.alpha = t;
            yield return null;
        }
        if (blackFadeCanvasGroup != null)
            blackFadeCanvasGroup.alpha = 1f;

        // Small delay before loading scene
        yield return new WaitForSeconds(sceneLoadDelay);

        // Load the menu scene
        SceneManager.LoadScene(menuSceneName);
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            keywordRecognizer.Stop();
    }
}