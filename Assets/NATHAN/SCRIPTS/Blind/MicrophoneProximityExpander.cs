using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class MicrophoneProximityExpander : MonoBehaviour
{
    [Header("Microphone Settings")]
    public string microphoneDevice = null;          // Leave null to use default
    public float sensitivity = 0.1f;                // Lower = more sensitive
    public float humThreshold = 0.02f;              // Minimum volume to be considered "humming"

    [Header("Proximity Expansion")]
    public float baseEndDistance = 3.0f;             // Normal black distance
    public float maxEndDistance = 6.0f;              // Max when humming loud
    public float expansionSpeed = 2.0f;              // How fast radius expands
    public float returnSpeed = 1.5f;                 // How fast it shrinks back

    [Header("Smoothing")]
    public float volumeSmoothing = 0.1f;             // Time window for averaging

    [Header("References")]
    public GlobalProximityShaderManager shaderManager;

    private AudioSource audioSource;
    private float currentVolume = 0f;
    private float targetEndDistance;
    private float currentEndDistance;
    private int sampleWindow = 64;
    private bool isMicrophoneInitialized = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            Debug.LogError("MicrophoneProximityExpander needs an AudioSource component.");
            return;
        }

        StartCoroutine(InitializeMicrophone());
    }

    IEnumerator InitializeMicrophone()
    {
        yield return Application.RequestUserAuthorization(UserAuthorization.Microphone);

        if (!Application.HasUserAuthorization(UserAuthorization.Microphone))
        {
            Debug.LogWarning("Microphone permission denied.");
            yield break;
        }

        if (string.IsNullOrEmpty(microphoneDevice))
        {
            if (Microphone.devices.Length == 0)
            {
                Debug.LogError("No microphone found!");
                yield break;
            }
            microphoneDevice = Microphone.devices[0];
        }

        audioSource.clip = Microphone.Start(microphoneDevice, true, 1, AudioSettings.outputSampleRate);
        audioSource.loop = true;
        audioSource.mute = true;   // Don't hear yourself
        while (!(Microphone.GetPosition(microphoneDevice) > 0)) { }
        audioSource.Play();

        isMicrophoneInitialized = true;
        Debug.Log($"Microphone '{microphoneDevice}' ready.");
    }

    void Update()
    {
        if (!isMicrophoneInitialized) return;

        float micVolume = GetAverageVolume();
        currentVolume = Mathf.Lerp(currentVolume, micVolume, 1f / volumeSmoothing * Time.deltaTime);

        float humIntensity = 0f;
        if (currentVolume > humThreshold)
            humIntensity = Mathf.Clamp01((currentVolume - humThreshold) / sensitivity);

        targetEndDistance = Mathf.Lerp(baseEndDistance, maxEndDistance, humIntensity);

        float speed = humIntensity > 0 ? expansionSpeed : returnSpeed;
        currentEndDistance = Mathf.MoveTowards(currentEndDistance, targetEndDistance, speed * Time.deltaTime);

        // Apply to shader manager – it will update all objects on its next cycle
        if (shaderManager != null)
            shaderManager.currentEndDistance = currentEndDistance;
    }

    float GetAverageVolume()
    {
        int micPos = Microphone.GetPosition(microphoneDevice);
        if (micPos < sampleWindow) return 0f;

        float[] samples = new float[sampleWindow];
        audioSource.clip.GetData(samples, micPos - sampleWindow);

        float sum = 0f;
        foreach (float s in samples) sum += s * s;
        return Mathf.Sqrt(sum / sampleWindow);
    }

    void OnDestroy()
    {
        if (isMicrophoneInitialized && Microphone.IsRecording(microphoneDevice))
            Microphone.End(microphoneDevice);
    }
}