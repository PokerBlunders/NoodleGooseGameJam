using UnityEngine;

public class AudioFadeOut : MonoBehaviour
{
    public AudioSource audioSource;   // The audio source to fade
    public float delay = 3f;          // Time before fade begins
    public float fadeDuration = 2f;   // How long the fade takes

    private bool isFading = false;
    private float startVolume;

    void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        startVolume = audioSource.volume;
        Invoke(nameof(StartFade), delay);
    }

    void StartFade()
    {
        if (!isFading)
            StartCoroutine(FadeOut());
    }

    System.Collections.IEnumerator FadeOut()
    {
        isFading = true;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeDuration);
            yield return null;
        }

        audioSource.volume = 0f;
    }
}
