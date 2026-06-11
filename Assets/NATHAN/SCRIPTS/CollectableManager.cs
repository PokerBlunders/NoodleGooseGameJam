using UnityEngine;
using TMPro; // remove if not using TextMeshPro
using System;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("UI")]
    public TextMeshProUGUI collectibleText; // optional
    public string textFormat = "Score: {0}";

    [Header("Target")]
    public int targetCollectibles = 10;      // number needed to trigger event
    public event Action OnTargetReached;    // event fired when target is hit

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
                Debug.Log("Target reached! " + totalCollected + " / " + targetCollectibles);
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
        // optional: play sound, particle effect, etc.
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