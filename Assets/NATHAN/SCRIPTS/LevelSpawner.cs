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
    [Tooltip("String of letters (S, E, M, R) defining level order. Example: 'SEEMRSEEMRSEEMRSEEMR' (19 letters)")]
    public string levelSequence = "SEEMRSEEMRSEEMRSEEMR";
    private int currentLevelIndex = 0;
    private bool allLevelsSpawned = false;

    [Header("Spawn Settings")]
    public Transform player;
    public float spawnTriggerDistance = 20f;   // how close player must be to last chunk to spawn next (was spawnDistanceAhead)
    public int maxActiveChunks = 3;
    public float chunkLength = 15f;            // length of each chunk in Z direction
    public float initialSpawnOffset = 20f;     // how far ahead of player to place the first chunk

    [Header("Debug")]
    public bool logLevelChanges = true;

    private List<GameObject> activeChunks = new List<GameObject>();
    private float nextSpawnZ;
    private bool gameWon = false;

    void Start()
    {
        if (player == null)
        {
            Debug.LogError("LevelSpawner: Player Transform not assigned!");
            return;
        }

        // Start placing first chunk this far ahead of the player
        nextSpawnZ = player.position.z + initialSpawnOffset;

        // Pre‑spawn two chunks so the player has something to run on
        SpawnNextChunk();
        SpawnNextChunk();
    }

    void Update()
    {
        if (gameWon) return;

        // Remove chunks that are far behind the player
        CleanupPassedChunks();

        // Spawn new chunks if we haven't finished the sequence and we are below the active limit
        if (!allLevelsSpawned && activeChunks.Count < maxActiveChunks)
        {
            float lastChunkEndZ = GetLastChunkEndZ();
            // Spawn when the player gets within trigger distance of the end of the last chunk
            if (lastChunkEndZ - player.position.z < spawnTriggerDistance)
            {
                SpawnNextChunk();
            }
        }

        // If all levels are spawned and no chunks remain active, win
        if (allLevelsSpawned && activeChunks.Count == 0 && !gameWon)
        {
            WinGame();
        }
    }

    void SpawnNextChunk()
    {
        if (currentLevelIndex >= levelSequence.Length)
        {
            allLevelsSpawned = true;
            return;
        }

        char letter = levelSequence[currentLevelIndex];
        GameObject prefabToSpawn = GetPrefabByLetter(letter);

        if (prefabToSpawn == null)
        {
            Debug.LogError($"No prefab for letter '{letter}' at index {currentLevelIndex}");
            return;
        }

        Vector3 spawnPos = new Vector3(0, 0, nextSpawnZ);
        GameObject newChunk = Instantiate(prefabToSpawn, spawnPos, Quaternion.identity);
        activeChunks.Add(newChunk);

        if (logLevelChanges)
            Debug.Log($"Spawned level {currentLevelIndex}: '{letter}' at Z = {nextSpawnZ}");

        // Move the spawn position forward by the chunk length for the next piece
        nextSpawnZ += chunkLength;
        currentLevelIndex++;

        if (currentLevelIndex >= levelSequence.Length)
            allLevelsSpawned = true;
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
                Debug.LogWarning($"Unknown letter '{letter}'");
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
            // Destroy if the player is more than 5 units past the chunk's end
            if (chunkEndZ < player.position.z - 5f)
            {
                Destroy(activeChunks[i]);
                activeChunks.RemoveAt(i);
            }
        }
    }

    void WinGame()
    {
        gameWon = true;
        Debug.Log("🎉 You completed all levels! 🎉");
        // Add your win UI / scene change / pause here
        // Example: FindObjectOfType<GameManager>().ShowWinScreen();
    }
}