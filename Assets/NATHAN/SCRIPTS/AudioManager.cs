using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Sound Effects")]
    public AudioClip jumpSound;
    [Range(0f, 1f)] public float jumpVolume = 1f;
    public AudioClip slideSound;
    [Range(0f, 1f)] public float slideVolume = 1f;
    public AudioClip swapSound;
    [Range(0f, 1f)] public float swapVolume = 1f;
    public AudioClip leftRightSound;
    [Range(0f, 1f)] public float leftRightVolume = 1f;
    public AudioClip deathSound;
    [Range(0f, 1f)] public float deathVolume = 1f;
    public AudioClip collectibleSound;
    [Range(0f, 1f)] public float collectibleVolume = 1f;
    public AudioClip footstepSound;
    [Range(0f, 1f)] public float footstepVolume = 1f;

    [Header("Footstep Settings")]
    public AudioSource footstepSource;   // optional, created if missing

    private AudioSource audioSource;

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
            return;
        }
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
            footstepSource.loop = true;
            footstepSource.playOnAwake = false;
        }
    }

    public void PlayJump() => PlaySFX(jumpSound, jumpVolume);
    public void PlaySlide() => PlaySFX(slideSound, slideVolume);
    public void PlaySwap() => PlaySFX(swapSound, swapVolume);
    public void PlayLeftRight() => PlaySFX(leftRightSound, leftRightVolume);
    public void PlayDeath() => PlaySFX(deathSound, deathVolume);
    public void PlayCollectible() => PlaySFX(collectibleSound, collectibleVolume);

    public void StartFootsteps()
    {
        if (footstepSource != null && footstepSound != null && !footstepSource.isPlaying)
        {
            footstepSource.clip = footstepSound;
            footstepSource.volume = footstepVolume;
            footstepSource.Play();
        }
    }

    public void StopFootsteps()
    {
        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
    }

    private void PlaySFX(AudioClip clip, float volume)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip, volume);
    }
}