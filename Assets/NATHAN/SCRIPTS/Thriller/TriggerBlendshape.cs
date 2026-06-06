using UnityEngine;
using System.Collections;

public class TriggerBlendshape : MonoBehaviour
{
    public SkinnedMeshRenderer skinnedMesh;
    public string blendshapeName = "Smile";
    public float delay = 2f;                // initial delay before blendshape activation
    public float fadeInDuration = 1f;
    public float holdDuration = 0.5f;
    public float fadeOutDuration = 1f;
    public float targetValue = 100f;

    [Header("Material Change (Optional)")]
    public Renderer materialTarget;          // object whose material to change
    public Material newMaterial;             // material to apply after delay
    public float materialChangeDelay = 20f;  // time to wait before switching material
    public bool revertMaterialAfterSequence = false; // if true, revert to original after blendshape sequence ends

    private int blendshapeIndex = -1;
    private Material originalMaterial;
    private bool materialChanged = false;

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

        // Store original material if needed
        if (materialTarget != null && newMaterial != null && revertMaterialAfterSequence)
            originalMaterial = materialTarget.material;

        // Start blendshape sequence after its own delay
        Invoke(nameof(StartSequence), delay);

        // Start material change timer
        if (materialTarget != null && newMaterial != null && materialChangeDelay > 0f)
            Invoke(nameof(ChangeMaterial), materialChangeDelay);
    }

    void StartSequence()
    {
        StartCoroutine(FullBlendSequence());
    }

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

        // Optionally revert material after the whole sequence
        if (revertMaterialAfterSequence && originalMaterial != null && materialTarget != null)
            materialTarget.material = originalMaterial;
    }
}