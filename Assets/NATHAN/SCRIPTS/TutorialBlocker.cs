using UnityEngine;
using System.Collections.Generic;

public class TutorialBlocker : MonoBehaviour
{
    [Header("Accepted Voice Commands (variants)")]
    public List<string> acceptedWords = new List<string>();

    [Header("UI / Visual Feedback")]
    public GameObject showOnActivate;
    public GameObject hideOnComplete;

    [Header("Behaviour")]
    public bool destroyOnComplete = true;

    private MovementController playerMovement;

    void Start()
    {
        if (acceptedWords == null || acceptedWords.Count == 0)
        {
            acceptedWords = new List<string>
            {
                "right", "rite", "righ", "ryt", "reight", "raight",
                "left", "lef", "lft", "lept", "leff", "laf",
                "jump", "jmp", "jomp", "jup", "jamp", "jum",
                "slide", "slid", "slyde", "slie", "sligh",
                "swap", "swop", "swp", "sap", "swapp"
            };
        }

        if (showOnActivate != null) showOnActivate.SetActive(false);
        if (hideOnComplete != null) hideOnComplete.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        playerMovement = other.GetComponent<MovementController>();
        if (playerMovement == null) return;

        if (showOnActivate != null)
            showOnActivate.SetActive(true);

        Time.timeScale = 0f;
        playerMovement.isFrozen = true;

        // Immediately stop footsteps when the game pauses
        if (AudioManager.Instance != null)
            AudioManager.Instance.StopFootsteps();

        playerMovement.OnCommandExecuted += OnCommandExecuted;
    }

    private void OnCommandExecuted(string spoken)
    {
        foreach (string word in acceptedWords)
        {
            if (spoken == word)
            {
                if (hideOnComplete != null)
                    hideOnComplete.SetActive(false);
                if (showOnActivate != null && showOnActivate != hideOnComplete)
                    showOnActivate.SetActive(false);

                Time.timeScale = 1f;
                playerMovement.isFrozen = false;
                playerMovement.OnCommandExecuted -= OnCommandExecuted;

                // Footsteps will automatically restart when the player moves again
                if (destroyOnComplete)
                    Destroy(gameObject);
                return;
            }
        }
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
        if (playerMovement != null)
            playerMovement.OnCommandExecuted -= OnCommandExecuted;
    }
}