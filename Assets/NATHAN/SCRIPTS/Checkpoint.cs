using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Visual Feedback (optional)")]
    public GameObject activationEffect; // particle effect or visual to show it was activated

    private bool isActivated = false;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isActivated)
        {
            isActivated = true;
            CheckpointManager.Instance.SetCheckpoint(other.transform.position);

            if (activationEffect != null)
                Instantiate(activationEffect, transform.position, Quaternion.identity);
        }
    }
}