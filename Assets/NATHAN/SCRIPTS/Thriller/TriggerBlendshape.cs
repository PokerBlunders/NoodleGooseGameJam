using UnityEngine;
using System.Collections;

public class TriggerBlendshape : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public string blendshapeName = "Smile";
    public float delay = 2f;                // seconds into music to start blendshape
    public float fadeInDuration = 1f;
    public float holdDuration = 0.5f;
    public float fadeOutDuration = 1f;
    public float targetValue = 100f;

    [Header("Material Change (Optional)")]
    public Renderer materialTarget;
    public Material newMaterial;
    public float materialChangeDelay = 20f;  // seconds into music to change material
    public bool revertMaterialAfterSequence = false;

    [Header("Music Source")]
    public AudioSource musicSource;

    private int blendshapeIndex = -1;
    private Material originalMaterial;
    private bool materialChanged = false;
    private double musicStartDspTime = -1;
    private bool musicStarted = false;

    void Start()
    {
        if (skinnedMesh == null)
            skinnedMesh = GetComponent<SkinnedMeshRenderer>();

        if (skinnedMesh == null || skinnedMesh.sharedMesh == null)
        {
            Debug.LogError("SkinnedMeshRenderer or sharedMesh is missing.");
            enabled = false;
            return;
        }

        blendshapeIndex = skinnedMesh.sharedMesh.GetBlendShapeIndex(blendshapeName);
        if (blendshapeIndex < 0)
        {
            Debug.LogError($"Blendshape '{blendshapeName}' not found.");
            enabled = false;
            return;
        }

        if (materialTarget != null && newMaterial != null && revertMaterialAfterSequence)
            originalMaterial = materialTarget.material;

        if (musicSource == null)
            musicSource = FindObjectOfType<AudioSource>();

        StartCoroutine(WaitForMusicStartThenSchedule());
    }

    IEnumerator WaitForMusicStartThenSchedule()
    {
        // Wait until music is playing and capture its start DSP time
        while (musicSource == null || !musicSource.isPlaying)
            yield return null;
        musicStartDspTime = AudioSettings.dspTime - musicSource.time;
        musicStarted = true;

        // Schedule blendshape sequence
        if (delay > 0)
            StartCoroutine(ExecuteAtMusicTime(delay, StartSequence));
        else
            StartSequence();

        // Schedule material change
        if (materialTarget != null && newMaterial != null && materialChangeDelay > 0)
            StartCoroutine(ExecuteAtMusicTime(materialChangeDelay, ChangeMaterial));
    }

    IEnumerator ExecuteAtMusicTime(float musicTime, System.Action action)
    {
        double targetDsp = musicStartDspTime + musicTime;
        while (AudioSettings.dspTime < targetDsp)
            yield return null;
        action?.Invoke();
    }

    void StartSequence() => StartCoroutine(FullBlendSequence());

    void ChangeMaterial()
    {
        if (materialTarget != null && newMaterial != null && !materialChanged)
        {
            materialTarget.material = newMaterial;
            materialChanged = true;
        }
    }

    IEnumerator FullBlendSequence()
    {
        // Fade in
        float startValue = skinnedMesh.GetBlendShapeWeight(blendshapeIndex);
        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeInDuration);
            float newValue = Mathf.Lerp(startValue, targetValue, t);
            skinnedMesh.SetBlendShapeWeight(blendshapeIndex, newValue);
            yield return null;
        }
        skinnedMesh.SetBlendShapeWeight(blendshapeIndex, targetValue);

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade out
        elapsed = 0f;
        startValue = targetValue;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeOutDuration);
            float newValue = Mathf.Lerp(startValue, 0f, t);
            skinnedMesh.SetBlendShapeWeight(blendshapeIndex, newValue);
            yield return null;
        }
        skinnedMesh.SetBlendShapeWeight(blendshapeIndex, 0f);

        if (revertMaterialAfterSequence && originalMaterial != null && materialTarget != null)
            materialTarget.material = originalMaterial;
    }
}