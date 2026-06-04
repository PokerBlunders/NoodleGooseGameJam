using System;
using UnityEngine;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
using UnityEngine.Windows.Speech;
#endif

[RequireComponent(typeof(CharacterController))]
public class ImprovedSoundPlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float turnAngle = 45f;

    [Header("语音移动时长")]
    [SerializeField] private float voiceMoveDuration = 0.5f;

    [Header("跳跃 / 重力")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -18f;

    // 内部组件引用
    private CharacterController controller;

    // 垂直速度
    private float verticalVelocity;
    private bool wantsToJump = false;

    // 语音移动倒计时
    private float voiceMoveTimer = 0f;
    private Vector3 voiceMoveDirection = Vector3.zero;

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private KeywordRecognizer keywordRecognizer;
#endif

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        InitializeVoice();
    }

    private void InitializeVoice()
    {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
        // ---------- 关键词设置 ----------
        // 使用 Low 置信度提高响应速度，若误触发多可改回 Medium
        ConfidenceLevel confidence = ConfidenceLevel.Low;

        // 关键词列表（已将 "fuck" 替换为 "left" 以避免误识别）
        string[] keywords = { "forward", "backward", "left", "right", "jump" };

        keywordRecognizer = new KeywordRecognizer(keywords, confidence);
        keywordRecognizer.OnPhraseRecognized += OnVoiceRecognized;
        keywordRecognizer.Start();

        Debug.Log($"语音控制已激活 | 置信度: {confidence} | 指令: forward, backward, left, right, jump");
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private void OnVoiceRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log($"<color=#00ff88>识别到: {args.text}</color> (置信度: {args.confidence})");

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

            case "left":
                // 左转：替换原来的 "fuck"
                transform.Rotate(0, -turnAngle, 0);
                break;

            case "right":
                // 右转
                transform.Rotate(0, turnAngle, 0);
                break;

            case "jump":
                // 请求跳跃，会在 Update 中的物理检查阶段执行
                wantsToJump = true;
                break;
        }
    }
#endif

    private void Update()
    {
        // === 1. 键盘输入 ===
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 move = (transform.right * h + transform.forward * v).normalized;

        // === 2. 语音移动覆盖（优先级高于键盘） ===
        if (voiceMoveTimer > 0f)
        {
            move = voiceMoveDirection;
            voiceMoveTimer -= Time.deltaTime;
        }

        // === 3. 重力与跳跃 ===
        bool isGrounded = controller.isGrounded;

        // 着地时重置垂直速度（保持轻微向下压，确保 isGrounded 稳定）
        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // 语音触发的跳跃（仅在着地时生效）
        if (isGrounded && wantsToJump)
        {
            // 跳跃物理公式：v = sqrt(2 * jumpHeight * |gravity|)
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            wantsToJump = false;
            Debug.Log("Jump executed!");
        }

        // 施加重力加速度
        verticalVelocity += gravity * Time.deltaTime;

        // === 4. 合成最终移动 ===
        Vector3 finalVelocity = move * walkSpeed;
        finalVelocity.y = verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
    private void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
        {
            keywordRecognizer.Stop();
            keywordRecognizer.Dispose();
        }
    }
#endif
}