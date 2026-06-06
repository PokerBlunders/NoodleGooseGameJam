using UnityEngine;
using System.Collections;

public class DelayedActivator : MonoBehaviour
{
    [Header("Target")]
    public GameObject targetObject;

    [Header("Timing (seconds into music)")]
    public float activateAtMusicTime = 2f;   // time in the music to activate

    [Header("Music Source")]
    public AudioSource musicSource;

    private double musicStartDspTime = -1;

    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogWarning("Target object not assigned!");
            enabled = false;
            return;
        }

        if (musicSource == null)
            musicSource = FindObjectOfType<AudioSource>();

        StartCoroutine(WaitForMusicThenActivate());
    }

    IEnumerator WaitForMusicThenActivate()
    {
        // Wait until music is playing and capture its start DSP time
        while (musicSource == null || !musicSource.isPlaying)
            yield return null;
        musicStartDspTime = AudioSettings.dspTime - musicSource.time;

        // Schedule activation at the given music time
        double targetDsp = musicStartDspTime + activateAtMusicTime;
        while (AudioSettings.dspTime < targetDsp)
            yield return null;

        targetObject.SetActive(true);
    }
}