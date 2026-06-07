using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
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

    [Header("View Swap Animations & Blendshapes")]
    public SkinnedMeshRenderer characterMesh;
    public string blueBlendshapeName = "Blue";
    public string redBlendshapeName = "Red";
    public float blendshapeTransitionDuration = 0.2f;
    private Coroutine blendshapeCoroutine = null;

    [Header("Obstacle")]
    public string obstacleTag = "Obstacle";

    [Header("Vosk Settings")]
    public string ModelFolderName = "vosk-model-small-en-us-0.15";
    public VoiceProcessor VoiceProcessor;
    private Model _model;
    private VoskRecognizer _recognizer;
    private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new ConcurrentQueue<short[]>();
    private readonly ConcurrentQueue<string> _threadedResultQueue = new ConcurrentQueue<string>();
    private bool _running;

    [Serializable] private struct VoskJsonBridge { public string text; }

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
        if (animator == null) animator = GetComponent<Animator>();
    }

    void Start()
    {
        targetX = (currentLane - 1) * laneDistance;
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

        // 初始化 Vosk
        StartVoskStt();

        if (animator != null) animator.SetBool("isRunning", true);
        if (ViewSwapper.Instance != null) ViewSwapper.Instance.OnViewChanged += OnViewSwapped;
    }

    private void StartVoskStt()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, ModelFolderName);
        _model = new Model(modelPath);
        _recognizer = new VoskRecognizer(_model, 16000.0f);

        VoiceProcessor.OnFrameCaptured += OnFrameCapturedHandler;
        VoiceProcessor.StartRecording(16000, 512, false);

        _running = true;
        Task.Run(ThreadedWork);
        Debug.Log("[Vosk] 引擎已启动");
    }

    private void OnFrameCapturedHandler(short[] samples) => _threadedBufferQueue.Enqueue(samples);

    private async Task ThreadedWork()
    {
        while (_running)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] samples))
            {
                if (_recognizer.AcceptWaveform(samples, samples.Length))
                    _threadedResultQueue.Enqueue(_recognizer.Result());
            }
            await Task.Delay(10);
        }
    }

    void Update()
    {
        // 键盘输入保留
        if (Input.GetKeyDown(KeyCode.A)) MoveLeft();
        if (Input.GetKeyDown(KeyCode.D)) MoveRight();
        if (Input.GetKeyDown(KeyCode.Space)) RequestJump();
        if (Input.GetKeyDown(KeyCode.LeftControl)) RequestSlide();
        if (Input.GetKeyDown(KeyCode.Q)) TriggerSwap();

        // 处理 Vosk 识别结果
        while (_threadedResultQueue.TryDequeue(out string result))
        {
            string command = JsonUtility.FromJson<VoskJsonBridge>(result).text.ToLower();
            if (string.IsNullOrEmpty(command)) continue;

            Debug.Log($"[识别指令]: {command}");
            if (command.Contains("left")) MoveLeft();
            else if (command.Contains("right")) MoveRight();
            else if (command.Contains("jump")) RequestJump();
            else if (command.Contains("slide")) RequestSlide();
            else if (command.Contains("swap")) TriggerSwap();
        }

        // 移动逻辑
        float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        rb.position = new Vector3(newX, rb.position.y, rb.position.z);

        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        rb.linearVelocity += Vector3.down * gravity * Time.deltaTime;
        Vector3 vel = rb.linearVelocity;
        vel.z = forwardSpeed;
        rb.linearVelocity = vel;

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

        if (isSliding) { slideTimer -= Time.deltaTime; if (slideTimer <= 0f) EndSlide(); }
    }

    // --- 动作方法保持不变 ---
    void MoveLeft() { if (currentLane > 0) { currentLane--; targetX = (currentLane - 1) * laneDistance; animator?.SetTrigger("Left"); } }
    void MoveRight() { if (currentLane < 2) { currentLane++; targetX = (currentLane - 1) * laneDistance; animator?.SetTrigger("Right"); } }
    void RequestJump() { if (isGrounded && !isSliding) jumpRequested = true; else jumpBufferTimer = jumpBufferTime; }
    void RequestSlide() { if (!isSliding && isGrounded && !jumpRequested) StartSlide(); else jumpBufferTimer = jumpBufferTime; }
    void StartSlide() { isSliding = true; slideTimer = slideDuration; if (capsuleCollider) { capsuleCollider.height = originalHeight - slideHeightReduction; capsuleCollider.center = new Vector3(originalCenter.x, originalCenter.y - (slideHeightReduction * 0.5f), originalCenter.z); } animator?.SetTrigger("Slide"); }
    void EndSlide() { isSliding = false; if (capsuleCollider) { capsuleCollider.height = originalHeight; capsuleCollider.center = originalCenter; } }
    void TriggerSwap() { ViewSwapper.Instance?.ToggleView(); }
    void OnViewSwapped(ViewSwapper.ViewMode newView) { /* ... 保持原有逻辑 ... */ }

    // --- 资源清理 ---
    void OnDestroy()
    {
        _running = false;
        VoiceProcessor?.StopRecording();
        _recognizer?.Dispose();
        _model?.Dispose();
    }

    // ... 其余逻辑 (OnCollisionEnter, SmoothBlendshapeTransition 等保持不变)
}