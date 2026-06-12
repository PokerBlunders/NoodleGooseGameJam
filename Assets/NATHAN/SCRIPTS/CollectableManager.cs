using UnityEngine;
using TMPro;
using System;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI collectibleText;
    public string textFormat = "Score: {0}";

    [Header("Target")]
    public int targetCollectibles = 10;
    public event Action OnTargetReached;

    private int totalCollected = 0;
    private bool targetReached = false;

    public int TotalCollected
    {
        get => totalCollected;
        private set
        {
            totalCollected = value;
            UpdateUI();
            if (!targetReached && totalCollected >= targetCollectibles)
            {
                targetReached = true;
                OnTargetReached?.Invoke();
                Debug.Log(".");
            }
        }
    }

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

    void Start()
    {
        UpdateUI();
    }

    public void AddCollectible(int value = 1)
    {
        TotalCollected += value;
        // Play collectible sound
        if (AudioManager.Instance != null)
            AudioManager.Instance.PlayCollectible();
    }

    public void ResetCollectibles()
    {
        TotalCollected = 0;
        targetReached = false;
    }

    private void UpdateUI()
    {
        if (collectibleText != null)
            collectibleText.text = string.Format(textFormat, TotalCollected);
    }
}