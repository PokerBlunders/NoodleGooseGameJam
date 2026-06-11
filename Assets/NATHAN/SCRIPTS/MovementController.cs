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

    [Header("Voice Recognition (Fuzzy)")]
    public int fuzzyThreshold = 3;

    [Header("Death")]
    public float deathAnimationDuration = 1f;
    private bool isDying = false;

    [Header("Menu Command")]
    public string menuSceneName = "MainMenu";  // Name of your menu scene
    private bool isLoadingMenu = false;

    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, System.Action> keywordActions = new Dictionary<string, System.Action>();
    private List<string> baseCommands = new List<string>();
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

        AddCommandVariants("left", MoveLeft,
            "lef", "lft", "lept", "leff", "laf", "leaft", "lefet", "leftt", "levt", "lep", "lefth", "lefht", "lefty", "leftht");
        AddCommandVariants("right", MoveRight,
            "rite", "righ", "ryt", "reight", "raight", "rightt", "rigt", "riht", "ryte", "writ", "rait", "rith", "righht", "ryght");
        AddCommandVariants("jump", RequestJump,
            "jmp", "jomp", "jup", "jamp", "jum", "jimp", "jmup", "jumpp", "jumb", "jumpa", "jumpe", "jumpk", "jamb");
        AddCommandVariants("slide", RequestSlide,
            "slid", "slyde", "slie", "sligh", "sliede", "slidd", "slidee", "slidy", "slyd", "slad", "slidde", "slith", "slight", "sliid");
        AddCommandVariants("swap", TriggerSwap,
            "swop", "swp", "sap", "swapp", "swape", "swab", "swep", "swup", "swip", "swaap", "swaph");
        AddCommandVariants("menu", LoadMenu,
            "men", "menu", "mennu", "menue", "mnu"); // variants for "menu"

        baseCommands.Clear();
        baseCommands.AddRange(new[] { "left", "right", "jump", "slide", "swap", "menu" });

        HashSet<string> uniqueKeywords = new HashSet<string>(keywordActions.Keys);
        keywordRecognizer = new KeywordRecognizer(uniqueKeywords.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();

        if (animator != null)
            animator.SetBool("isRunning", true);

        if (ViewSwapper.Instance != null)
            ViewSwapper.Instance.OnViewChanged += OnViewSwapped;
    }

    private void AddCommandVariants(string mainWord, System.Action action, params string[] variants)
    {
        if (!keywordActions.ContainsKey(mainWord))
            keywordActions.Add(mainWord, action);
        foreach (string v in variants)
        {
            if (!keywordActions.ContainsKey(v))
                keywordActions.Add(v, action);
        }
    }

    void Update()
    {
        if (isDying || isLoadingMenu) return; // ignore input while dying or loading menu

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
        if (isDying || isLoadingMenu) return;

        rb.linearVelocity += Vector3.down * gravity * Time.deltaTime;

        Vector3 vel = rb.linearVelocity;
        if (isSliding)
            vel.z = forwardSpeed * slideSpeedMultiplier;
        else if (!isGrounded)
            vel.z = forwardSpeed * airSpeedMultiplier;
        else
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
            yield break;

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

    // ---------- Death & Respawn ----------
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(obstacleTag) && !isDying && !isLoadingMenu)
            StartDeathSequence();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(obstacleTag) && !isDying && !isLoadingMenu)
            StartDeathSequence();
    }

    private void StartDeathSequence()
    {
        if (isDying) return;
        isDying = true;

        rb.linearVelocity = Vector3.zero;
        if (animator != null)
            animator.SetTrigger("Died");

        StartCoroutine(DeathSequenceCoroutine());
    }

    private IEnumerator DeathSequenceCoroutine()
    {
        yield return new WaitForSeconds(deathAnimationDuration);

        if (CheckpointManager.Instance != null && CheckpointManager.Instance.HasCheckpoint())
        {
            yield return StartCoroutine(ScreenFadeManager.Instance.FadeOut());

            Vector3 respawnPos = CheckpointManager.Instance.GetCheckpointPosition();
            transform.position = respawnPos;
            rb.linearVelocity = Vector3.zero;
            isSliding = false;
            jumpRequested = false;
            currentLane = 1;
            targetX = (currentLane - 1) * laneDistance;
            if (animator != null)
                animator.SetTrigger("Respawn");

            isDying = false;
            StartCoroutine(ScreenFadeManager.Instance.FadeIn());
        }
        else
        {
            yield return StartCoroutine(ScreenFadeManager.Instance.FadeOut());
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    // ---------- Menu command ----------
    private void LoadMenu()
    {
        if (isLoadingMenu || isDying) return;
        isLoadingMenu = true;

        // Optionally stop input immediately
        rb.linearVelocity = Vector3.zero;
        if (animator != null)
            animator.SetTrigger("Died"); // optional: play a quick death/exit animation

        StartCoroutine(LoadMenuCoroutine());
    }

    private IEnumerator LoadMenuCoroutine()
    {
        // Fade out using ScreenFadeManager
        yield return StartCoroutine(ScreenFadeManager.Instance.FadeOut());
        // Small extra delay (optional)
        yield return new WaitForSeconds(0.2f);
        SceneManager.LoadScene(menuSceneName);
    }

    // ---------- Voice recognition ----------
    void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        string spoken = args.text.ToLower();

        if (keywordActions.ContainsKey(spoken))
        {
            keywordActions[spoken]();
            return;
        }

        int bestDist = int.MaxValue;
        string bestCmd = null;
        foreach (string cmd in baseCommands)
        {
            int dist = LevenshteinDistance(spoken, cmd);
            if (dist < bestDist && dist <= fuzzyThreshold)
            {
                bestDist = dist;
                bestCmd = cmd;
            }
        }

        if (bestCmd != null && keywordActions.ContainsKey(bestCmd))
            keywordActions[bestCmd]();
    }

    private int LevenshteinDistance(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) return b?.Length ?? 0;
        if (string.IsNullOrEmpty(b)) return a.Length;
        int[,] d = new int[a.Length + 1, b.Length + 1];
        for (int i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (int j = 0; j <= b.Length; j++) d[0, j] = j;
        for (int i = 1; i <= a.Length; i++)
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                d[i, j] = Mathf.Min(d[i - 1, j] + 1, d[i, j - 1] + 1, d[i - 1, j - 1] + cost);
            }
        return d[a.Length, b.Length];
    }

    void OnDestroy()
    {
        if (keywordRecognizer != null && keywordRecognizer.IsRunning)
            keywordRecognizer.Stop();
    }
}