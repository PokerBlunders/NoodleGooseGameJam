using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class NavigationalSoundSource : MonoBehaviour
{
    [Header("Sound Settings")]
    public AudioClip soundClip;
    public bool playOnStart = true;
    public bool loop = true;

    [Header("Audio Source Settings")]
    [Range(0f, 1f)] public float volume = 0.8f;
    [Range(0.1f, 50f)] public float minDistance = 1f;
    [Range(1f, 100f)] public float maxDistance = 20f;
    public AnimationCurve volumeCurve = AnimationCurve.Linear(0, 1, 1, 0); // volume vs distance

    [Header("Optional: Trigger Zone (for one‑shot sounds)")]
    public bool useTriggerZone = false;
    public bool oneShotOnTriggerEnter = true;
    [Tooltip("If true, sound plays once when player enters, then stops.")]
    public bool destroyAfterPlaying = false;

    private AudioSource audioSource;
    private bool hasPlayedTriggerSound = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource();
    }

    void Start()
    {
        if (playOnStart && !useTriggerZone)
        {
            PlaySound();
        }
    }

    void ConfigureAudioSource()
    {
        audioSource.clip = soundClip;
        audioSource.volume = volume;
        audioSource.loop = loop;
        audioSource.spatialBlend = 1f;           // Full 3D sound
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.rolloffMode = AudioRolloffMode.Custom;
        audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, volumeCurve);
        audioSource.dopplerLevel = 0f;           // No doppler for navigation
    }

    public void PlaySound()
    {
        if (soundClip == null)
        {
            Debug.LogWarning($"No sound clip assigned to {gameObject.name}");
            return;
        }

        if (loop)
            audioSource.Play();
        else
            audioSource.PlayOneShot(soundClip, volume);
    }

    public void StopSound()
    {
        audioSource.Stop();
    }

    // Optional trigger zone – attach a Collider (set as Trigger) to the same GameObject
    void OnTriggerEnter(Collider other)
    {
        if (!useTriggerZone) return;
        if (!oneShotOnTriggerEnter) return;
        if (hasPlayedTriggerSound) return;

        if (other.CompareTag("Player"))
        {
            PlaySound();
            hasPlayedTriggerSound = true;

            if (destroyAfterPlaying && !loop)
                Destroy(gameObject, soundClip.length);
        }
    }

    // Editor helper: show sound range
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, minDistance);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, maxDistance);
    }
}