using System;
using System.Collections;
using System.Collections.Concurrent;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Vosk;

public class VoskSpeechToTextNEW : MonoBehaviour
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

    [Serializable]
    private struct VoskJsonBridge
    {
        public string text;
    }

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
        if (groundCheck == null)
        {
            Debug.LogWarning("GroundCheck not assigned. Movement may not work correctly.");
            return;
        }

        targetX = (currentLane - 1) * laneDistance;
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

        // Find VoiceProcessor if not assigned
        if (VoiceProcessor == null)
            VoiceProcessor = FindObjectOfType<VoiceProcessor>();

        if (VoiceProcessor == null)
            Debug.LogError("[Vosk] VoiceProcessor component not found in scene!");
        else
            StartVoskStt();

        if (animator != null) animator.SetBool("isRunning", true);
        if (ViewSwapper.Instance != null) ViewSwapper.Instance.OnViewChanged += OnViewSwapped;
    }

    private void StartVoskStt()
    {
        try
        {
            string modelPath = Path.Combine(Application.streamingAssetsPath, ModelFolderName);
            if (!Directory.Exists(modelPath))
            {
                Debug.LogError($"[Vosk] Model folder not found: {modelPath}");
                return;
            }
            _model = new Model(modelPath);
            _recognizer = new VoskRecognizer(_model, 16000.0f);

            VoiceProcessor.OnFrameCaptured += OnFrameCapturedHandler;
            // Use continuous audio (autoDetect = false) for reliable recognition
            VoiceProcessor.StartRecording(16000, 512, false);

            _running = true;
            Task.Run(ThreadedWork);
            Debug.Log("[Vosk] Engine started – continuous audio, partial results enabled.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[Vosk] Initialisation failed: {e.Message}");
        }
    }

    private void OnFrameCapturedHandler(short[] samples)
    {
        if (_running)
            _threadedBufferQueue.Enqueue(samples);
    }

    private async Task ThreadedWork()
    {
        while (_running)
        {
            if (_threadedBufferQueue.TryDequeue(out short[] samples) && _recognizer != null)
            {
                _recognizer.AcceptWaveform(samples, samples.Length);
                string partial = _recognizer.PartialResult();
                if (!string.IsNullOrEmpty(partial) && partial.Length > 2)
                {
                    _threadedResultQueue.Enqueue(partial);
                }
            }
            await Task.Delay(1);
        }
    }

    void Update()
    {
        // Keyboard fallbacks
        if (Input.GetKeyDown(KeyCode.A)) MoveLeft();
        if (Input.GetKeyDown(KeyCode.D)) MoveRight();
        if (Input.GetKeyDown(KeyCode.Space)) RequestJump();
        if (Input.GetKeyDown(KeyCode.LeftControl)) RequestSlide();
        if (Input.GetKeyDown(KeyCode.Q)) TriggerSwap();

        // Process Vosk partial results
        while (_threadedResultQueue.TryDequeue(out string result))
        {
            try
            {
                var bridge = JsonUtility.FromJson<VoskJsonBridge>(result);
                if (bridge.text == null || string.IsNullOrEmpty(bridge.text)) continue;
                string command = bridge.text.ToLower().Trim();
                ProcessCommand(command);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Parse error: {e.Message}");
            }
        }

        // Lane movement
        float newX = Mathf.Lerp(transform.position.x, targetX, laneSwitchSpeed * Time.deltaTime);
        rb.position = new Vector3(newX, rb.position.y, rb.position.z);

        if (coyoteTimer > 0) coyoteTimer -= Time.deltaTime;
        if (jumpBufferTimer > 0) jumpBufferTimer -= Time.deltaTime;
    }

    private void ProcessCommand(string command)
    {
        if (command.Contains("left")) MoveLeft();
        else if (command.Contains("right")) MoveRight();
        else if (command.Contains("jump")) RequestJump();
        else if (command.Contains("slide")) RequestSlide();
        else if (command.Contains("swap")) TriggerSwap();
    }

    void FixedUpdate()
    {
        rb.linearVelocity += Vector3.down * gravity * Time.deltaTime;

        Vector3 vel = rb.linearVelocity;
        vel.z = forwardSpeed;
        rb.linearVelocity = vel;

        bool newGrounded = false;
        if (groundCheck != null)
            newGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        if (newGrounded)
            coyoteTimer = coyoteTime;
        isGrounded = newGrounded;

        if ((jumpBufferTimer > 0 || jumpRequested) && coyoteTimer > 0 && !isSliding)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, jumpForce, rb.linearVelocity.z);
            jumpBufferTimer = 0f;
            jumpRequested = false;
            coyoteTimer = 0f;
            if (animator != null)
                animator.SetTrigger("Jump");
        }

        if (isSliding)
        {
            slideTimer -= Time.deltaTime;
            if (slideTimer <= 0f)
                EndSlide();
        }
    }

    void MoveLeft()
    {
        if (currentLane > 0)
        {
            currentLane--;
            targetX = (currentLane - 1) * laneDistance;
            if (animator != null)
                animator.SetTrigger("Left");
        }
    }

    void MoveRight()
    {
        if (currentLane < 2)
        {
            currentLane++;
            targetX = (currentLane - 1) * laneDistance;
            if (animator != null)
                animator.SetTrigger("Right");
        }
    }

    void RequestJump()
    {
        if (isGrounded && !isSliding)
            jumpRequested = true;
        else
            jumpBufferTimer = jumpBufferTime;
    }

    void RequestSlide()
    {
        if (!isSliding && isGrounded && !jumpRequested)
            StartSlide();
        else
            jumpBufferTimer = jumpBufferTime;
    }

    void StartSlide()
    {
        isSliding = true;
        slideTimer = slideDuration;
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalHeight - slideHeightReduction;
            Vector3 newCenter = originalCenter;
            newCenter.y = originalCenter.y - (slideHeightReduction * 0.5f);
            capsuleCollider.center = newCenter;
        }
        if (animator != null)
            animator.SetTrigger("Slide");
    }

    void EndSlide()
    {
        isSliding = false;
        if (capsuleCollider != null)
        {
            capsuleCollider.height = originalHeight;
            capsuleCollider.center = originalCenter;
        }
    }

    void TriggerSwap()
    {
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
        if (blueIdx == -1 || redIdx == -1)
        {
            Debug.LogWarning("Blendshape names not found");
            yield break;
        }

        float startBlue = characterMesh.GetBlendShapeWeight(blueIdx);
        float startRed = characterMesh.GetBlendShapeWeight(redIdx);
        float targetBlue = (newView == ViewSwapper.ViewMode.Blue) ? 100f : 0f;
        float targetRed = (newView == ViewSwapper.ViewMode.Red) ? 100f : 0f;

        float elapsed = 0f;
        while (elapsed < blendshapeTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / blendshapeTransitionDuration);
            float newBlue = Mathf.Lerp(startBlue, targetBlue, t);
            float newRed = Mathf.Lerp(startRed, targetRed, t);
            characterMesh.SetBlendShapeWeight(blueIdx, newBlue);
            characterMesh.SetBlendShapeWeight(redIdx, newRed);
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

    void OnDestroy()
    {
        _running = false;
        if (VoiceProcessor != null)
            VoiceProcessor.StopRecording();
        _recognizer?.Dispose();
        _model?.Dispose();
    }
}