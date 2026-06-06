using UnityEngine;
using System.Collections;

public class SmoothFollow : MonoBehaviour
{
    [Header("Targets")]
    public Transform targetA;
    public Transform targetB;

    [Header("Camera Settings for Target A")]
    public float distanceA = -1f;
    public float heightA = 0.1f;
    public float smoothA = 1f;

    [Header("Camera Settings for Target B")]
    public float distanceB = -3.5f;
    public float heightB = 0.5f;
    public float smoothB = 0.2f;

    [Header("Target Position Blend")]
    public float targetBlendMusicTime = 5f;   // music time when blend starts
    public float targetBlendDuration = 2f;
    private float targetBlend = 0f;
    private float targetBlendStartTime = -1f;

    [Header("Camera Settings Blend")]
    public float settingsBlendMusicTime = 5f; // music time when settings blend starts
    public float settingsBlendDuration = 2f;
    private float settingsBlend = 0f;
    private float settingsBlendStartTime = -1f;

    [Header("Music Source")]
    public AudioSource musicSource;

    private Vector3 velocity = Vector3.zero;
    private double musicStartDspTime = -1;
    private bool musicStarted = false;

    void Start()
    {
        if (targetA == null || targetB == null)
        {
            Debug.LogError("Assign both targets!");
            enabled = false;
            return;
        }

        if (musicSource == null)
            musicSource = FindObjectOfType<AudioSource>();

        StartCoroutine(WaitForMusicAndSchedule());
    }

    IEnumerator WaitForMusicAndSchedule()
    {
        while (musicSource == null || !musicSource.isPlaying)
            yield return null;
        musicStartDspTime = AudioSettings.dspTime - musicSource.time;
        musicStarted = true;

        // Schedule target blend start
        StartCoroutine(ExecuteAtMusicTime(targetBlendMusicTime, StartTargetBlend));
        // Schedule settings blend start
        StartCoroutine(ExecuteAtMusicTime(settingsBlendMusicTime, StartSettingsBlend));
    }

    IEnumerator ExecuteAtMusicTime(float musicTime, System.Action action)
    {
        double targetDsp = musicStartDspTime + musicTime;
        while (AudioSettings.dspTime < targetDsp)
            yield return null;
        action?.Invoke();
    }

    void StartTargetBlend()
    {
        targetBlendStartTime = Time.time;
        velocity = Vector3.zero;
    }

    void StartSettingsBlend()
    {
        settingsBlendStartTime = Time.time;
    }

    void LateUpdate()
    {
        // Update target blend factor
        if (targetBlendStartTime >= 0f)
        {
            float elapsed = Time.time - targetBlendStartTime;
            if (elapsed >= targetBlendDuration)
            {
                targetBlend = 1f;
                targetBlendStartTime = -1f;
            }
            else
            {
                targetBlend = Mathf.Clamp01(elapsed / targetBlendDuration);
            }
        }

        // Update settings blend factor
        if (settingsBlendStartTime >= 0f)
        {
            float elapsed = Time.time - settingsBlendStartTime;
            if (elapsed >= settingsBlendDuration)
            {
                settingsBlend = 1f;
                settingsBlendStartTime = -1f;
            }
            else
            {
                settingsBlend = Mathf.Clamp01(elapsed / settingsBlendDuration);
            }
        }

        // Blend target position & forward
        Vector3 targetPos = Vector3.Lerp(targetA.position, targetB.position, targetBlend);
        Vector3 targetForward = Vector3.Slerp(targetA.forward, targetB.forward, targetBlend).normalized;

        // Blend camera settings
        float curDist = Mathf.Lerp(distanceA, distanceB, settingsBlend);
        float curHeight = Mathf.Lerp(heightA, heightB, settingsBlend);
        float curSmooth = Mathf.Lerp(smoothA, smoothB, settingsBlend);

        Vector3 desiredPos = targetPos - targetForward * curDist + Vector3.up * curHeight;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref velocity, curSmooth);
        transform.LookAt(targetPos);
    }
}