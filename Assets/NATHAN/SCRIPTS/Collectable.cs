using UnityEngine;

public class Collectible : MonoBehaviour
{
    [Header("Settings")]
    public int value = 1; // optional: can be used for different scores

    private void OnTriggerEnter(Collider other)
    {
        // Make sure the player has the tag "Player"
        if (other.CompareTag("Player"))
        {
            if (CollectibleManager.Instance != null)
                CollectibleManager.Instance.AddCollectible();

            // Optional: add sound effect, particle, or animation here

            Destroy(gameObject); // remove the collectible
        }
    }
}