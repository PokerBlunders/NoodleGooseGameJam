using UnityEngine;
using System.Collections;

public class MusicSyncManager : MonoBehaviour
{
    [Header("Music Source")]
    public AudioSource musicSource;

    [Header("Animation / Visual Events")]
    public Animator characterAnimator;
    public string startAnimationTrigger = "Start";  // trigger to start character animation

    [Header("Optional: Additional events at specific music times")]
    public float[] eventTimes;          // times in seconds (from music start)
    public string[] eventTriggers;      // corresponding Animator triggers (same length)

    [Header("Initial Delay")]
    public float initialDelay = 2f;      // how long to wait before the scheduled start

    private bool scheduled = false;

    void Start()
    {
        if (musicSource == null)
            musicSource = GetComponent<AudioSource>();

        if (musicSource == null)
        {
            return;
        }

        // Schedule the start
        StartCoroutine(ScheduleStart());
    }

    IEnumerator ScheduleStart()
    {
        // Wait for initial delay (using real‑world time to be consistent)
        float realStart = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - realStart < initialDelay)
            yield return null;

        double dspTime = AudioSettings.dspTime;
        musicSource.PlayScheduled(dspTime);

        // Trigger any immediate animation at the same exact time
        if (characterAnimator != null)
            StartCoroutine(ExecuteAtExactTime(dspTime, () => characterAnimator.SetTrigger(startAnimationTrigger)));

        // Schedule extra events at specific music times
        for (int i = 0; i < eventTimes.Length && i < eventTriggers.Length; i++)
        {
            double eventTime = dspTime + eventTimes[i];
            string trigger = eventTriggers[i];
            if (characterAnimator != null)
                StartCoroutine(ExecuteAtExactTime(eventTime, () => characterAnimator.SetTrigger(trigger)));
        }

        scheduled = true;
    }

    IEnumerator ExecuteAtExactTime(double targetDspTime, System.Action action)
    {
        while (AudioSettings.dspTime < targetDspTime)
            yield return null;
        action?.Invoke();
    }

    // Public method to get current music position (if needed by other scripts)
    public float GetMusicTime()
    {
        if (musicSource.isPlaying)
            return (float)(AudioSettings.dspTime - musicSource.time);
        else
            return 0f;
    }
}