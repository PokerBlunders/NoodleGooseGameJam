using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vosk;

public class MovementControllerVosk : MonoBehaviour
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

    [Header("Slide Speed Boost")]
    public float slideSpeedMultiplier = 1.5f;

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

    [Header("Air Control")]
    public float airSpeedMultiplier = 1.75f;

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
    private const float CommandCooldown = 0.15f;   // short cooldown prevents spam but keeps responsiveness

    public System.Action OnSwap;

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
        if (animator != null) animator.SetBool("isRunning", true);

        if (ViewSwapper.Instance != null)
            ViewSwapper.Instance.OnViewChanged += OnViewSwapped;

        try
        {
            string modelPath = Path.Combine(Application.streamingAssetsPath, ModelFolderName);
            if (!Directory.Exists(modelPath))
            {
                Debug.LogError("Vosk model not found at: " + modelPath);
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
            else
                Debug.LogError("VoiceProcessor component missing!");
        }
        catch (Exception e)
        {
            Debug.LogError("Vosk init failed: " + e.Message);
        }
    }

    private async Task ThreadedWork()
    {
        while (_running)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] samples))
            {
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
        // Keyboard fallbacks
        if (Input.GetKeyDown(KeyCode.A)) ExecuteCommand("left");
        if (Input.GetKeyDown(KeyCode.D)) ExecuteCommand("right");
        if (Input.GetKeyDown(KeyCode.Space)) ExecuteCommand("jump");
        if (Input.GetKeyDown(KeyCode.LeftControl)) ExecuteCommand("slide");
        if (Input.GetKeyDown(KeyCode.Q)) TriggerSwap();

        // Process Vosk results (immediate)
        while (_threadedResultQueue.TryDequeue(out string result))
        {
            var data = JsonUtility.FromJson<VoskJsonBridge>(result);
            string spoken = !string.IsNullOrEmpty(data.text) ? data.text.ToLower() : data.partial.ToLower();
            if (string.IsNullOrEmpty(spoken)) continue;

            if (spoken.Contains("left")) ExecuteCommand("left");
            else if (spoken.Contains("right")) ExecuteCommand("right");
            else if (spoken.Contains("jump")) ExecuteCommand("jump");
            else if (spoken.Contains("slide")) ExecuteCommand("slide");
            else if (spoken.Contains("swap")) TriggerSwap();
        }

        // Smooth lane movement
        float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        rb.position = new Vector3(newX, rb.position.y, rb.position.z);

        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        rb.linearVelocity += Vector3.down * gravity * Time.deltaTime;

        Vector3 vel = rb.linearVelocity;
        if (isSliding)
            vel.z = forwardSpeed * slideSpeedMultiplier;
        else if (!isGrounded)
            vel.z = forwardSpeed * airSpeedMultiplier;
        else
            vel.z = forwardSpeed;
        rb.linearVelocity = vel;

        bool newGrounded = (groundCheck != null) && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (newGrounded) coyoteTimer = coyoteTime;
        isGrounded = newGrounded;

        if ((jumpBufferTimer > 0 || jumpRequested) && coyoteTimer > 0 && !isSliding)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpRequested = false;
            coyoteTimer = 0f;
            animator?.SetTrigger("Jump");
        }

        if (coyoteTimer > 0) coyoteTimer -= Time.fixedDeltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.fixedDeltaTime;
        if (isSliding) { slideTimer -= Time.fixedDeltaTime; if (slideTimer <= 0f) EndSlide(); }
    }

    private void ExecuteCommand(string cmd)
    {
        // Very short cooldown to prevent spam but allow fast repeated different commands
        if (_lastCommandTime.TryGetValue(cmd, out float lastTime) && Time.time - lastTime < CommandCooldown)
            return;

        _lastCommandTime[cmd] = Time.time;

        switch (cmd)
        {
            case "left":
                if (currentLane > 0)
                {
                    currentLane--;
                    targetX = (currentLane - 1) * laneDistance;
                    animator?.SetTrigger("Left");
                }
                break;
            case "right":
                if (currentLane < 2)
                {
                    currentLane++;
                    targetX = (currentLane - 1) * laneDistance;
                    animator?.SetTrigger("Right");
                }
                break;
            case "jump":
                if (isGrounded && !isSliding)
                    jumpRequested = true;
                else
                    jumpBufferTimer = jumpBufferTime;
                break;
            case "slide":
                if (!isSliding && isGrounded && !jumpRequested)
                {
                    isSliding = true;
                    slideTimer = slideDuration;
                    if (capsuleCollider) capsuleCollider.height = originalHeight - slideHeightReduction;
                    animator?.SetTrigger("Slide");
                }
                else
                    jumpBufferTimer = jumpBufferTime;
                break;
        }
    }

    private void EndSlide()
    {
        isSliding = false;
        if (capsuleCollider) capsuleCollider.height = originalHeight;
    }

    private void TriggerSwap()
    {
        // Swap also respects cooldown (use the key "swap" in the dictionary)
        if (_lastCommandTime.TryGetValue("swap", out float lastTime) && Time.time - lastTime < CommandCooldown)
            return;
        _lastCommandTime["swap"] = Time.time;

        OnSwap?.Invoke();
        if (ViewSwapper.Instance != null)
            ViewSwapper.Instance.ToggleView();
    }

    void OnViewSwapped(ViewSwapper.ViewMode newView)
    {
        if (animator != null)
        {
            if (newView == ViewSwapper.ViewMode.Blue)
                animator.SetTrigger("SwapLeft");
            else
                animator.SetTrigger("SwapRight");
        }

        if (characterMesh != null)
        {
            if (blendshapeCoroutine != null)
                StopCoroutine(blendshapeCoroutine);
            blendshapeCoroutine = StartCoroutine(SmoothBlendshapeTransition(newView));
        }
    }

    IEnumerator SmoothBlendshapeTransition(ViewSwapper.ViewMode newView)
    {
        int blueIdx = characterMesh.sharedMesh.GetBlendShapeIndex(blueBlendshapeName);
        int redIdx = characterMesh.sharedMesh.GetBlendShapeIndex(redBlendshapeName);
        if (blueIdx == -1 || redIdx == -1) yield break;

        float startBlue = characterMesh.GetBlendShapeWeight(blueIdx);
        float startRed = characterMesh.GetBlendShapeWeight(redIdx);
        float targetBlue = (newView == ViewSwapper.ViewMode.Blue) ? 100f : 0f;
        float targetRed = (newView == ViewSwapper.ViewMode.Red) ? 100f : 0f;

        float elapsed = 0f;
        while (elapsed < blendshapeTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendshapeTransitionDuration);
            characterMesh.SetBlendShapeWeight(blueIdx, Mathf.Lerp(startBlue, targetBlue, t));
            characterMesh.SetBlendShapeWeight(redIdx, Mathf.Lerp(startRed, targetRed, t));
            yield return null;
        }
        characterMesh.SetBlendShapeWeight(blueIdx, targetBlue);
        characterMesh.SetBlendShapeWeight(redIdx, targetRed);
        blendshapeCoroutine = null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(obstacleTag))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(obstacleTag))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void OnDisable()
    {
        _running = false;
    }

    void OnDestroy()
    {
        _running = false;
        if (VoiceProcessor != null) VoiceProcessor.StopRecording();
        _recognizer?.Dispose();
        _model?.Dispose();
    }
}