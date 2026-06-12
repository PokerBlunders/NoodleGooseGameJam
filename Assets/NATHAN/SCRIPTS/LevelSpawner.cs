using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class LevelSpawner : MonoBehaviour
{
    [Header("Prefab Pools")]
    public GameObject[] startPrefabs;   // S
    public GameObject[] easyPrefabs;    // E
    public GameObject[] mediumPrefabs;  // M
    public GameObject[] rewardPrefabs;  // R

    [Header("Level Sequence")]
    [Tooltip("String of letters (S, E, M, R) defining level order.")]
    public string levelSequence = "SEEMRSEEMRSEEMRSEEMR";
    private int currentLevelIndex = 0;

    [Header("Spawn Settings")]
    public Transform player;
    public float spawnTriggerDistance = 20f;
    public int maxActiveChunks = 3;
    public float chunkLength = 15f;
    public float initialSpawnOffset = 20f;

    [Header("Looping")]
    public bool loopSequence = true;   // if true, repeats the sequence forever

    [Header("Debug")]
    public bool logLevelChanges = true;

    private List<GameObject> activeChunks = new List<GameObject>();
    private float nextSpawnZ;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError(".");
            return;
        }

        nextSpawnZ = player.position.z + initialSpawnOffset;
        SpawnNextChunk();
        SpawnNextChunk();
    }

    void Update()
    {
        CleanupPassedChunks();

        if (activeChunks.Count < maxActiveChunks)
        {
            float lastChunkEndZ = GetLastChunkEndZ();
            if (lastChunkEndZ - player.position.z < spawnTriggerDistance)
            {
                SpawnNextChunk();
            }
        }
    }

    void SpawnNextChunk()
    {
        if (currentLevelIndex >= levelSequence.Length)
        {
            if (loopSequence)
            {
                currentLevelIndex = 0;
                if (logLevelChanges)
                    Debug.Log(".");
            }
            else
            {
                // No more levels and no looping – nothing to spawn
                return;
            }
        }

        char letter = levelSequence[currentLevelIndex];
        GameObject prefabToSpawn = GetPrefabByLetter(letter);

        if (prefabToSpawn == null)
        {
            Debug.LogError($".");
            return;
        }

        Vector3 spawnPos = new Vector3(0, 0, nextSpawnZ);
        GameObject newChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        activeChunks.Add(newChunk);

        if (logLevelChanges)
            Debug.Log($".");

        nextSpawnZ += chunkLength;
        currentLevelIndex++;
    }

    GameObject GetPrefabByLetter(char letter)
    {
        switch (letter)
        {
            case 'S': return GetRandomPrefab(startPrefabs);
            case 'E': return GetRandomPrefab(easyPrefabs);
            case 'M': return GetRandomPrefab(mediumPrefabs);
            case 'R': return GetRandomPrefab(rewardPrefabs);
            default:
                return null;
        }
    }

    GameObject GetRandomPrefab(GameObject[] array)
    {
        if (array == null || array.Length == 0) return null;
        return array[Random.Range(0, array.Length)];
    }

    float GetLastChunkEndZ()
    {
        if (activeChunks.Count == 0) return nextSpawnZ;
        float maxZ = -Mathf.Infinity;
        foreach (var chunk in activeChunks)
        {
            if (chunk != null)
            {
                float chunkEnd = chunk.transform.position.z + chunkLength;
                if (chunkEnd > maxZ) maxZ = chunkEnd;
            }
        }
        return maxZ;
    }

    void CleanupPassedChunks()
    {
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            if (activeChunks[i] == null)
            {
                activeChunks.RemoveAt(i);
                continue;
            }

            float chunkEndZ = activeChunks[i].transform.position.z + chunkLength;
            if (chunkEndZ < player.position.z - 5f)
            {
                Destroy(activeChunks[i]);
                activeChunks.RemoveAt(i);
            }
        }
    }
}