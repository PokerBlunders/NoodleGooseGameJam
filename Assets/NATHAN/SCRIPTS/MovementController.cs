using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;
using UnityEngine.SceneManagement;

public class MovementController : MonoBehaviour
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

    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, System.Action> keywordActions = new Dictionary<string, System.Action>();
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
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    void Start()
    {
        if (groundCheck == null)
            return;

        targetX = (currentLane - 1) * laneDistance;
        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

        // Add main commands and common mispronunciation variants
        AddCommandVariants("left", MoveLeft, "lef", "lft", "lept", "leff");
        AddCommandVariants("right", MoveRight, "rite", "righ", "ryt", "reight");
        AddCommandVariants("jump", RequestJump, "jmp", "jomp", "jup", "jamp");
        AddCommandVariants("slide", RequestSlide, "slid", "slyde", "slie", "sligh");
        AddCommandVariants("swap", TriggerSwap, "swop", "swp", "sap", "swapp");

        keywordRecognizer = new KeywordRecognizer(keywordActions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();

        if (animator != null)
            animator.SetBool("isRunning", true);

        if (ViewSwapper.Instance != null)
            ViewSwapper.Instance.OnViewChanged += OnViewSwapped;
    }

    private void AddCommandVariants(string mainWord, System.Action action, params string[] variants)
    {
        // Add the main word
        if (!keywordActions.ContainsKey(mainWord))
            keywordActions.Add(mainWord, action);
        // Add each variant
        foreach (string v in variants)
        {
            if (!keywordActions.ContainsKey(v))
                keywordActions.Add(v, action);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A)) MoveLeft();
        if (Input.GetKeyDown(KeyCode.D)) MoveRight();
        if (Input.GetKeyDown(KeyCode.Space)) RequestJump();
        if (Input.GetKeyDown(KeyCode.LeftControl)) RequestSlide();
        if (Input.GetKeyDown(KeyCode.Q)) TriggerSwap();

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
        else
            Debug.LogWarning("ViewSwapper instance not found");
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

    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string word = args.text.ToLower();
        if (keywordActions.ContainsKey(word))
            keywordActions[word]();
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            keywordRecognizer.Stop();
    }
}