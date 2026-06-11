using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Windows.Speech;
using UnityEngine.SceneManagement;

public class VoiceLevelSlider : MonoBehaviour
{
    [Header("Audio Settings")]
    public string microphoneDevice = null;
    public int sampleRate = 16000;
    public float sensitivity = 2f;
    public float smoothSpeed = 10f;

    [Header("UI")]
    public Slider voiceSlider;
    public CanvasGroup uiCanvasGroup;
    public float uiFadeOutDuration = 0.5f;

    [Header("UI Movement")]
    public GameObject uiMoveObject;
    public Vector3 uiMoveOffset = new Vector3(0, -100f, 0);
    public bool useLocalPosition = true;
    private Vector3 uiMoveStartPos;

    [Header("Fade to Black")]
    public CanvasGroup blackFadeCanvasGroup;
    public float fadeInDuration = 0.5f;
    public float fadeToBlackDuration = 0.5f;

    [Header("Character Blendshapes")]
    public SkinnedMeshRenderer characterMesh;
    public string firstBlendshapeName = "Blink";
    public string secondBlendshapeName = "Smile";
    public float blendshapeTransitionDuration = 0.3f;

    [Header("Keyword Commands")]
    public List<AudioClip> speakSounds;
    public List<AudioClip> startSounds;
    public AudioSource audioSource;
    public string startSceneName = "Game";
    public float sceneLoadDelay = 0.5f;

    // New: Thriller command
    public string thrillerSceneName = "Thriller";
    private bool isProcessingThriller = false;

    private AudioClip microphoneClip;
    private float currentVolume = 0f;
    private KeywordRecognizer keywordRecognizer;
    private bool isProcessingStart = false;

    void Start()
    {
        if (voiceSlider == null)
            voiceSlider = GetComponent<Slider>();

        if (uiMoveObject != null)
        {
            uiMoveStartPos = useLocalPosition ? uiMoveObject.transform.localPosition : uiMoveObject.transform.position;
        }

        if (blackFadeCanvasGroup != null)
        {
            blackFadeCanvasGroup.alpha = 1f;
            StartCoroutine(FadeIn());
        }
        else
        {
            InitializeMicrophoneAndRecognizer();
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            blackFadeCanvasGroup.alpha = 1f - t;
            yield return null;
        }
        blackFadeCanvasGroup.alpha = 0f;
        InitializeMicrophoneAndRecognizer();
    }

    private void InitializeMicrophoneAndRecognizer()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone found!");
            enabled = false;
            return;
        }
        if (string.IsNullOrEmpty(microphoneDevice))
            microphoneDevice = Microphone.devices[0];
        microphoneClip = Microphone.Start(microphoneDevice, true, 1, sampleRate);
        while (Microphone.GetPosition(microphoneDevice) <= 0) { }

        // Add "thriller" to keywords
        string[] keywords = { "speak", "start", "thriller" };
        keywordRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Medium);
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();

        if (audioSource == null && (speakSounds.Count > 0 || startSounds.Count > 0))
            audioSource = gameObject.AddComponent<AudioSource>();

        Debug.Log("Ready for voice commands.");
    }

    void Update()
    {
        if (microphoneClip == null) return;

        int micPos = Microphone.GetPosition(microphoneDevice);
        if (micPos <= 0) return;
        int sampleWindow = 256;
        int startPos = micPos - sampleWindow;
        if (startPos < 0) startPos = 0;
        float[] samples = new float[sampleWindow];
        microphoneClip.GetData(samples, startPos);
        float rms = 0f;
        for (int i = 0; i < samples.Length; i++)
            rms += samples[i] * samples[i];
        rms = Mathf.Sqrt(rms / sampleWindow);
        float rawVolume = Mathf.Clamp01(rms * sensitivity);
        currentVolume = Mathf.Lerp(currentVolume, rawVolume, smoothSpeed * Time.deltaTime);
        if (voiceSlider != null)
            voiceSlider.value = currentVolume;
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string word = args.text.ToLower();
        switch (word)
        {
            case "speak":
                if (speakSounds != null && speakSounds.Count > 0 && audioSource != null)
                {
                    AudioClip clip = speakSounds[Random.Range(0, speakSounds.Count)];
                    audioSource.PlayOneShot(clip);
                }
                break;
            case "start":
                if (!isProcessingStart)
                {
                    if (startSounds != null && startSounds.Count > 0 && audioSource != null)
                    {
                        AudioClip clip = startSounds[Random.Range(0, startSounds.Count)];
                        audioSource.PlayOneShot(clip);
                    }
                    StartCoroutine(StartSequence());
                }
                break;
            case "thriller":
                if (!isProcessingThriller)
                {
                    isProcessingThriller = true;
                    StartCoroutine(ThrillerSequence());
                }
                break;
        }
    }

    private IEnumerator ThrillerSequence()
    {
        // Simple fade to black then load scene
        if (blackFadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeToBlackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeToBlackDuration);
                blackFadeCanvasGroup.alpha = t;
                yield return null;
            }
            blackFadeCanvasGroup.alpha = 1f;
        }

        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(thrillerSceneName);
    }

    private IEnumerator StartSequence()
    {
        isProcessingStart = true;

        // --- Fade out UI and move object ---
        if (uiCanvasGroup != null || uiMoveObject != null)
        {
            float elapsed = 0f;
            float startAlpha = (uiCanvasGroup != null) ? uiCanvasGroup.alpha : 1f;
            Vector3 startPos = uiMoveStartPos;
            Vector3 targetPos = startPos + uiMoveOffset;

            while (elapsed < uiFadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / uiFadeOutDuration);

                if (uiCanvasGroup != null)
                    uiCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);

                if (uiMoveObject != null)
                {
                    Vector3 newPos = Vector3.Lerp(startPos, targetPos, t);
                    if (useLocalPosition)
                        uiMoveObject.transform.localPosition = newPos;
                    else
                        uiMoveObject.transform.position = newPos;
                }
                yield return null;
            }

            if (uiCanvasGroup != null)
            {
                uiCanvasGroup.alpha = 0f;
                uiCanvasGroup.interactable = false;
                uiCanvasGroup.blocksRaycasts = false;
            }
            if (uiMoveObject != null)
            {
                if (useLocalPosition)
                    uiMoveObject.transform.localPosition = targetPos;
                else
                    uiMoveObject.transform.position = targetPos;
            }
        }

        // 2. First blendshape to 0
        if (characterMesh != null && !string.IsNullOrEmpty(firstBlendshapeName))
        {
            int idx = characterMesh.sharedMesh.GetBlendShapeIndex(firstBlendshapeName);
            if (idx >= 0)
            {
                float startVal = characterMesh.GetBlendShapeWeight(idx);
                float targetVal = 0f;
                float elapsed = 0f;
                while (elapsed < blendshapeTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / blendshapeTransitionDuration);
                    float newVal = Mathf.Lerp(startVal, targetVal, t);
                    characterMesh.SetBlendShapeWeight(idx, newVal);
                    yield return null;
                }
                characterMesh.SetBlendShapeWeight(idx, targetVal);
            }
        }

        // 3. Second blendshape to 1
        if (characterMesh != null && !string.IsNullOrEmpty(secondBlendshapeName))
        {
            int idx = characterMesh.sharedMesh.GetBlendShapeIndex(secondBlendshapeName);
            if (idx >= 0)
            {
                float startVal = characterMesh.GetBlendShapeWeight(idx);
                float targetVal = 100f;
                float elapsed = 0f;
                while (elapsed < blendshapeTransitionDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / blendshapeTransitionDuration);
                    float newVal = Mathf.Lerp(startVal, targetVal, t);
                    characterMesh.SetBlendShapeWeight(idx, newVal);
                    yield return null;
                }
                characterMesh.SetBlendShapeWeight(idx, targetVal);
            }
        }

        // 4. Fade to black
        if (blackFadeCanvasGroup != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeToBlackDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeToBlackDuration);
                blackFadeCanvasGroup.alpha = t;
                yield return null;
            }
            blackFadeCanvasGroup.alpha = 1f;
        }

        // 5. Extra delay then load scene
        yield return new WaitForSeconds(sceneLoadDelay);
        SceneManager.LoadScene(startSceneName);
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            keywordRecognizer.Stop();
        if (microphoneClip != null && Microphone.IsRecording(microphoneDevice))
            Microphone.End(microphoneDevice);
    }
}