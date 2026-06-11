using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance { get; private set; }

    private Vector3 lastCheckpointPosition;
    private bool hasCheckpoint = false;

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
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        lastCheckpointPosition = position;
        hasCheckpoint = true;
    }

    public Vector3 GetCheckpointPosition()
    {
        return lastCheckpointPosition;
    }

    public bool HasCheckpoint()
    {
        return hasCheckpoint;
    }

    // Call this when starting a new level or resetting progress
    public void ClearCheckpoint()
    {
        hasCheckpoint = false;
        lastCheckpointPosition = Vector3.zero;
    }
}