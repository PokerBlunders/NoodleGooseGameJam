using UnityEngine;
using TMPro;
using System;
using UnityEngine.SceneManagement;

public class CollectibleManager : MonoBehaviour
{
    public static CollectibleManager Instance { get; private set; }

    [Header("UI")]
    public string collectibleTextName = "CollectibleText"; // Name of the GameObject with TMP_Text
    private TextMeshProUGUI collectibleText; // will be found dynamically
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

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Find the UI text component by name
        GameObject textObj = GameObject.Find(collectibleTextName);
        if (textObj != null)
        {
            collectibleText = textObj.GetComponent<TextMeshProUGUI>();
            if (collectibleText == null)
                Debug.LogWarning($"CollectibleManager: GameObject '{collectibleTextName}' found but has no TextMeshProUGUI component.");
        }
        else
        {
            // It's okay if the scene doesn't have this UI element
            collectibleText = null;
        }
        UpdateUI();
    }

    void Start()
    {
        // Also try to find on start in case scene is already loaded when this object is created
        GameObject textObj = GameObject.Find(collectibleTextName);
        if (textObj != null)
            collectibleText = textObj.GetComponent<TextMeshProUGUI>();
        UpdateUI();
    }

    public void AddCollectible(int value = 1)
    {
        TotalCollected += value;
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