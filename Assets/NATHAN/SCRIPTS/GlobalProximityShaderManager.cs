using UnityEngine;
using System.Collections.Generic;

public class GlobalProximityShaderManager : MonoBehaviour
{
    [Header("References")]
    public Transform player;

    [Header("Settings")]
    public float updateInterval = 0.1f;
    public bool refreshCacheEveryFrame = false; // Only true if you add/remove objects at runtime

    private List<Renderer> cachedRenderers = new List<Renderer>();
    private float timer;
    private MaterialPropertyBlock propBlock;

    void Start()
    {
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
            else Debug.LogError("No player assigned or found with tag 'Player'");
        }

        propBlock = new MaterialPropertyBlock();
        RefreshCache();
    }

    void RefreshCache()
    {
        cachedRenderers.Clear();

        // New Unity API: FindObjectsByType with no sorting for better performance
        Renderer[] all = FindObjectsByType<Renderer>(FindObjectsSortMode.None);

        foreach (Renderer rend in all)
        {
            if (rend.sharedMaterial != null && rend.sharedMaterial.shader != null &&
                rend.sharedMaterial.shader.name == "Custom/ProximitySurface")
            {
                cachedRenderers.Add(rend);
            }
        }

        Debug.Log($"Cached {cachedRenderers.Count} renderers with proximity shader.");
    }

    void Update()
    {
        if (player == null) return;

        if (refreshCacheEveryFrame)
            RefreshCache();

        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        timer = 0f;

        Vector3 playerPos = player.position;
        foreach (Renderer rend in cachedRenderers)
        {
            if (rend == null) continue;
            rend.GetPropertyBlock(propBlock);
            propBlock.SetVector("_PlayerPosition", playerPos);
            rend.SetPropertyBlock(propBlock);
        }
    }

    // Call this if you spawn/destroy objects with the shader at runtime
    public void InvalidateCache() => RefreshCache();
}