using UnityEngine;

/// <summary>
/// Visual feedback showing player's sound emission radius.
/// </summary>
public class SoundVisualizer : MonoBehaviour
{
    public PlayerStealthState playerState;
    public GameObject soundRingPrefab;
    public float minRingScale = 0.5f;
    public float maxRingScale = 8f;

    private GameObject ringInstance;

    void Start()
    {
        if (playerState == null)
            playerState = GetComponent<PlayerStealthState>();

        if (soundRingPrefab != null)
        {
            ringInstance = Instantiate(soundRingPrefab, transform);
            ringInstance.transform.localPosition = Vector3.zero;
        }
    }

    void Update()
    {
        if (ringInstance != null)
        {
            float targetScale = playerState.isMoving ? playerState.soundRadius : 0f;
            Vector3 currentScale = ringInstance.transform.localScale;
            float newScale = Mathf.MoveTowards(currentScale.x, targetScale, 15f * Time.deltaTime);
            ringInstance.transform.localScale = new Vector3(newScale, 0.01f, newScale);
        }
    }
}