using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

[RequireComponent(typeof(CharacterController))]
public class ImprovedSoundPlayerController : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float turnAngle = 45f;
    [SerializeField] private float voiceMoveDuration = 0.5f;

    private CharacterController controller;
    private float verticalVelocity;
    private float voiceMoveTimer = 0f;
    private Vector3 voiceMoveDirection = Vector3.zero;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private KeywordRecognizer keywordRecognizer;
#endif

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        InitializeVoice();
    }

    private void InitializeVoice()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        // 修复：将 "tright" 改回 "right"
        string[] keywords = { "forward", "backward", "fuck", "right" };

        keywordRecognizer = new KeywordRecognizer(keywords, ConfidenceLevel.Medium);
        keywordRecognizer.OnPhraseRecognized += OnVoiceRecognized;
        keywordRecognizer.Start();
        Debug.Log("语音控制已激活: forward, backward, left, right");
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private void OnVoiceRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log($"识别到: {args.text}");

        switch (args.text)
        {
            case "forward":
                voiceMoveDirection = transform.forward;
                voiceMoveTimer = voiceMoveDuration;
                break;
            case "backward":
                voiceMoveDirection = -transform.forward;
                voiceMoveTimer = voiceMoveDuration;
                break;
            case "fuck":
                transform.Rotate(0, -turnAngle, 0);
                break;
            case "right":
                transform.Rotate(0, turnAngle, 0);
                break;
        }
    }
#endif

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // 只有键盘没动时才允许语音移动主导，避免冲突
        Vector3 move = (transform.right * h + transform.forward * v).normalized;

        if (voiceMoveTimer > 0)
        {
            move = voiceMoveDirection; // 语音控制时直接覆盖方向
            voiceMoveTimer -= Time.deltaTime;
        }

        if (controller.isGrounded && verticalVelocity < 0) verticalVelocity = -2f;
        verticalVelocity += -18f * Time.deltaTime;

        Vector3 finalVelocity = move * walkSpeed;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    // ... OnDestroy 方法保持不变 ...
}