using UnityEngine;

public class PlayerControllero : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 2f;

    private CharacterController controller;
    private Camera playerCamera;
    private float verticalVelocity;
    private float xRotation = 0f;

    // 效果变量
    private float currentSpeedMultiplier = 1f;
    private float currentJumpMultiplier = 1f;
    private float speedBoostTimer = 0f;
    private float jumpBoostTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        HandleEffects();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        playerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleMovement()
    {
        bool isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // 水平移动
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;

        float currentSpeed = walkSpeed * currentSpeedMultiplier;
        if (Input.GetKey(KeyCode.LeftShift))
            currentSpeed = runSpeed * currentSpeedMultiplier;

        controller.Move(move * currentSpeed * Time.deltaTime);

        // 跳跃 - 使用你提供的逻辑
        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * currentJumpMultiplier * -2f * gravity);

        // 应用重力
        verticalVelocity += gravity * Time.deltaTime;
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }

    void HandleEffects()
    {
        if (speedBoostTimer > 0)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0)
                currentSpeedMultiplier = 1f;
        }

        if (jumpBoostTimer > 0)
        {
            jumpBoostTimer -= Time.deltaTime;
            if (jumpBoostTimer <= 0)
                currentJumpMultiplier = 1f;
        }
    }

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        currentSpeedMultiplier = multiplier;
        speedBoostTimer = duration;
        Debug.Log($"速度提升！x{multiplier}");
    }

    public void ApplyJumpBoost(float multiplier, float duration)
    {
        currentJumpMultiplier = multiplier;
        jumpBoostTimer = duration;
        Debug.Log($"跳跃提升！x{multiplier}");
    }
}