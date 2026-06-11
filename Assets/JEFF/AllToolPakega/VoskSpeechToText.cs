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

    [Header("Vosk Settings")]
    public string ModelFolderName = "vosk-model-small-en-us-0.15";
    public VoiceProcessor VoiceProcessor;
    private Model _model;
    private VoskRecognizer _recognizer;
    private bool _running;
    private readonly ConcurrentQueue<short[]> _threadedBufferQueue = new ConcurrentQueue<short[]>();
    private readonly ConcurrentQueue<string> _threadedResultQueue = new ConcurrentQueue<string>();

    [Serializable] private struct VoskJsonBridge { public string text; public string partial; }

    // 优化：将冷却时间设为 0.25s，兼顾识别防抖与操作灵敏度
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
        // 1. 确保位置初始化正确
        targetX = (currentLane - 1) * laneDistance;
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);
        if (animator != null) animator.SetBool("isRunning", true);

        // 2. 使用 try-catch 包裹初始化，防止底层 DLL 崩溃导致游戏完全无法启动
        try
        {
            string modelPath = Path.Combine(Application.streamingAssetsPath, ModelFolderName);
            Debug.Log("尝试加载模型路径: " + modelPath);

            // 检查模型文件是否存在
            if (!Directory.Exists(modelPath))
            {
                Debug.LogError("【Vosk错误】模型目录不存在！请检查 StreamingAssets 目录下是否有名为 " + ModelFolderName + " 的文件夹");
                return;
            }

            // 初始化 Vosk
            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);

            // 启动录音
            if (VoiceProcessor != null)
            {
                VoiceProcessor.OnFrameCaptured += (samples) => _threadedBufferQueue.Enqueue(samples);
                VoiceProcessor.StartRecording(16000, 512, false);
                _running = true;
                Task.Run(ThreadedWork);
                Debug.Log("Vosk 初始化成功！");
            }
            else
            {
                Debug.LogError("【Vosk错误】未在 Inspector 中分配 VoiceProcessor 组件！");
            }
        }
        catch (Exception e)
        {
            // 如果 DLL 加载失败或内存报错，这里会打印堆栈信息
            Debug.LogError("【Vosk严重错误】初始化异常: " + e.Message + "\n" + e.StackTrace);
        }
    }

    private void StartVoskStt()
    {
        string modelPath = Path.Combine(Application.streamingAssetsPath, ModelFolderName);
        _model = new Model(modelPath);
        _recognizer = new VoskRecognizer(_model, 16000.0f);

        VoiceProcessor.OnFrameCaptured += (samples) => _threadedBufferQueue.Enqueue(samples);
        VoiceProcessor.StartRecording(16000, 512, false);

        _running = true;
        Task.Run(ThreadedWork);
    }

    private async Task ThreadedWork()
    {
        while (_running)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] samples))
            {
                bool isFullResult = _recognizer.AcceptWaveform(samples, samples.Length);
                string json = isFullResult ? _recognizer.Result() : _recognizer.PartialResult();
                if (!string.IsNullOrEmpty(json)) _threadedResultQueue.Enqueue(json);
            }
            await Task.Delay(10);
        }
    }

    void Update()
    {
        // 键盘输入
        if (Input.GetKeyDown(KeyCode.A)) ExecuteCommand("left");
        if (Input.GetKeyDown(KeyCode.D)) ExecuteCommand("right");
        if (Input.GetKeyDown(KeyCode.Space)) ExecuteCommand("jump");
        if (Input.GetKeyDown(KeyCode.LeftControl)) ExecuteCommand("slide");

        // 处理语音识别结果
        while (_threadedResultQueue.TryDequeue(out string result))
        {
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
        float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        rb.position = new Vector3(newX, rb.position.y, rb.position.z);
    }

    void FixedUpdate()
    {
        // 物理逻辑：确保重力和前进力生效
        rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y - (gravity * Time.fixedDeltaTime), forwardSpeed);

        // 地面检测
        bool newGrounded = (groundCheck != null) && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (newGrounded) coyoteTimer = coyoteTime;
        isGrounded = newGrounded;

        // 跳跃执行
        if ((jumpBufferTimer > 0 || jumpRequested) && coyoteTimer > 0 && !isSliding)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;
            coyoteTimer = 0;
            animator?.SetTrigger("Jump");
        }

        // 计时器更新
        if (coyoteTimer > 0) coyoteTimer -= Time.fixedDeltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.fixedDeltaTime;
        if (isSliding) { slideTimer -= Time.fixedDeltaTime; if (slideTimer <= 0f) EndSlide(); }
    }

    private void ExecuteCommand(string cmd)
    {
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

    void OnDestroy()
    {
        _running = false;
        VoiceProcessor?.StopRecording();
        _recognizer?.Dispose();
        _model?.Dispose();
    }
}