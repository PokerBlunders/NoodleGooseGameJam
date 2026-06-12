using UnityEngine;
using UnityEngine.UI;

public class VoiceSlider : MonoBehaviour
{
    [Header("Microphone Settings")]
    public string microphoneDevice = null;      // leave empty for default
    public int sampleRate = 16000;
    public float sensitivity = 2f;
    public float smoothSpeed = 10f;

    [Header("UI")]
    public Slider voiceSlider;                  // drag your UI Slider here

    private AudioClip microphoneClip;
    private float currentVolume = 0f;

    void Start()
    {
        if (voiceSlider == null)
            voiceSlider = GetComponent<Slider>();

        if (Microphone.devices.Length == 0)
        {
            enabled = false;
            return;
        }

        if (string.IsNullOrEmpty(microphoneDevice))
            microphoneDevice = Microphone.devices[0];

        microphoneClip = Microphone.Start(microphoneDevice, true, 1, sampleRate);
        while (Microphone.GetPosition(microphoneDevice) <= 0) { } // wait for mic to start
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
        // Use unscaledDeltaTime so smoothing works even when Time.timeScale = 0
        currentVolume = Mathf.Lerp(currentVolume, rawVolume, smoothSpeed * Time.unscaledDeltaTime);

        if (voiceSlider != null)
            voiceSlider.value = currentVolume;
    }

    void OnDestroy()
    {
        if (microphoneClip != null && Microphone.IsRecording(microphoneDevice))
            Microphone.End(microphoneDevice);
    }
}