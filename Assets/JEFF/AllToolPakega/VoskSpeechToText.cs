using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Vosk;

public class VoskSpeechToText : MonoBehaviour
{
    [Header("Forward Movement")]
    public float forwardSpeed = 10f;

    [Header("Lane Switching")]
    public float laneDistance = 2.5f;
    public float laneSwitchSpeed = 12f;
    private int currentLane = 1;
    private float targetX;

    [Header("Jump & Gravity")]
    public float jumpForce = 6f;
    public float gravity = 18f;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;
    public Transform groundCheck;
    public float coyoteTime = 0.12f;
    public float jumpBufferTime = 0.1f;
    private float coyoteTimer = 0f;
    private float jumpBufferTimer = 0f;
    private bool jumpRequested = false;

    [Header("Slide")]
    public float slideDuration = 0.8f;
    public float slideHeightReduction = 0.5f;
    private bool isSliding = false;
    private float slideTimer = 0f;
    private float originalHeight;
    private Vector3 originalCenter;
    private CapsuleCollider capsuleCollider;

    [Header("Animation")]
    public Animator animator;

    [Header("Vosk Settings")]
    public string ModelFolderName = "vosk-model-small-en-us-0.15";
    public VoiceProcessor VoiceProcessor;
    private Model _model;
    private VoskRecognizer _recognizer;
    private bool _running;
    private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new ConcurrentQueue<short[]>();
    private readonly ConcurrentQueue<string> _threadedResultQueue = new ConcurrentQueue<string>();

    [Serializable] private struct VoskJsonBridge { public string text; public string partial; }

    private Dictionary<string, float> _lastCommandTime = new Dictionary<string, float>();
    private const float CooldownTime = 0.25f;

    private Rigidbody rb;
    private bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        if (capsuleCollider != null)
        {
            originalHeight = capsuleCollider.height;
            originalCenter = capsuleCollider.center;
        }
    }

    void Start()
    {
        targetX = (currentLane - 1) * laneDistance;
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        if (animator != null) animator.SetBool("isRunning", true);

        try
        {
            string modelPath = Path.Combine(Application.streamingAssetsPath, ModelFolderName);
            if (!Directory.Exists(modelPath))
            {
                Debug.LogError("【Vosk错误】模型目录不存在: " + modelPath);
                return;
            }

            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);

            if (VoiceProcessor != null)
            {
                VoiceProcessor.OnFrameCaptured += (samples) => _threadedBufferQueue.Enqueue(samples);
                VoiceProcessor.StartRecording(16000, 512, false);
                _running = true;
                Task.Run(ThreadedWork);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("【Vosk严重错误】初始化异常: " + e.Message);
        }
    }

    private async Task ThreadedWork()
    {
        while (_running)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] samples))
            {
                // 如果系统正在关闭，直接跳出
                if (!_running) break;

                bool isFullResult = _recognizer.AcceptWaveform(samples, samples.Length);
                string json = isFullResult ? _recognizer.Result() : _recognizer.PartialResult();
                if (!string.IsNullOrEmpty(json)) _threadedResultQueue.Enqueue(json);
            }
            await Task.Delay(10);
        }
    }

    void Update()
    {
        // 死亡后或对象被销毁时，立即停止处理
        if (this == null || !gameObject.activeInHierarchy) return;

        // 键盘输入
        if (Input.GetKeyDown(KeyCode.A)) ExecuteCommand("left");
        if (Input.GetKeyDown(KeyCode.D)) ExecuteCommand("right");
        if (Input.GetKeyDown(KeyCode.Space)) ExecuteCommand("jump");
        if (Input.GetKeyDown(KeyCode.LeftControl)) ExecuteCommand("slide");

        // 处理语音识别结果
        while (_threadedResultQueue.TryDequeue(out string result))
        {
            // 防御性编程：在处理队列时检查对象是否还存活
            if (this == null) break;

            var data = JsonUtility.FromJson<VoskJsonBridge>(result);
            string command = !string.IsNullOrEmpty(data.text) ? data.text.ToLower() : data.partial.ToLower();

            if (!string.IsNullOrEmpty(command))
            {
                if (command.Contains("left")) ExecuteCommand("left");
                else if (command.Contains("right")) ExecuteCommand("right");
                else if (command.Contains("jump")) ExecuteCommand("jump");
                else if (command.Contains("slide")) ExecuteCommand("slide");
            }
        }

        // 移动平滑插值
        if (rb != null)
        {
            float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
            rb.position = new Vector3(newX, rb.position.y, rb.position.z);
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y - (gravity * Time.fixedDeltaTime), forwardSpeed);

        bool newGrounded = (groundCheck != null) && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (newGrounded) coyoteTimer = coyoteTime;
        isGrounded = newGrounded;

        if ((jumpBufferTimer > 0 || jumpRequested) && coyoteTimer > 0 && !isSliding)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;
            coyoteTimer = 0;
            animator?.SetTrigger("Jump");
        }

        if (coyoteTimer > 0) coyoteTimer -= Time.fixedDeltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.fixedDeltaTime;
        if (isSliding) { slideTimer -= Time.fixedDeltaTime; if (slideTimer <= 0f) EndSlide(); }
    }

    private void ExecuteCommand(string cmd)
    {
        // 关键安全检查：如果对象即将被销毁，不要执行任何物理或动画操作
        if (this == null || animator == null) return;

        if (_lastCommandTime.ContainsKey(cmd) && Time.time - _lastCommandTime[cmd] < CooldownTime)
            return;

        _lastCommandTime[cmd] = Time.time;

        switch (cmd)
        {
            case "left": if (currentLane > 0) { currentLane--; targetX = (currentLane - 1) * laneDistance; animator?.SetTrigger("Left"); } break;
            case "right": if (currentLane < 2) { currentLane++; targetX = (currentLane - 1) * laneDistance; animator?.SetTrigger("Right"); } break;
            case "jump": if (isGrounded && !isSliding) jumpRequested = true; else jumpBufferTimer = jumpBufferTime; break;
            case "slide": if (!isSliding && isGrounded && !jumpRequested) { isSliding = true; slideTimer = slideDuration; if (capsuleCollider) capsuleCollider.height = originalHeight - slideHeightReduction; animator?.SetTrigger("Slide"); } break;
        }
    }

    private void EndSlide() { isSliding = false; if (capsuleCollider) capsuleCollider.height = originalHeight; }

    void OnDisable()
    {
        _running = false;
    }

    void OnDestroy()
    {
        _running = false;
        if (VoiceProcessor != null) VoiceProcessor.StopRecording();

        // 必须清理 Vosk 资源，否则会引起内存泄漏或崩溃
        if (_recognizer != null) { _recognizer.Dispose(); _recognizer = null; }
        if (_model != null) { _model.Dispose(); _model = null; }
    }
}