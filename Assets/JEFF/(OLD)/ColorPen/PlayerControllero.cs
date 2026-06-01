using UnityEngine;

public class PlayerControllero : MonoBehaviour
{
    [Header("移动设置")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float mouseSensitivity = 2f;

    [Header("下蹲设置")]
    public KeyCode crouchKey = KeyCode.LeftControl;   // 下蹲按键
    public float crouchHeight = 1f;                   // 下蹲时控制器高度
    public float crouchSpeed = 2.5f;                  // 下蹲移动速度
    public float cameraCrouchHeight = 0.8f;           // 下蹲时相机高度

    private CharacterController controller;
    private Camera playerCamera;
    private float verticalVelocity;
    private float xRotation = 0f;

    // 站立时的原始参数（运行时记录）
    private float standingHeight;
    private float cameraStandHeight;
    private bool isCrouching = false;

    // 效果变量
    private float currentSpeedMultiplier = 1f;
    private float currentJumpMultiplier = 1f;
    private float speedBoostTimer = 0f;
    private float jumpBoostTimer = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();

        // 记录站立参数
        standingHeight = controller.height;
        // 保证角色脚底在本地坐标 y=0
        if (Mathf.Abs(controller.center.y - standingHeight * 0.5f) > 0.01f)
        {
            controller.center = new Vector3(controller.center.x, standingHeight * 0.5f, controller.center.z);
        }

        if (playerCamera != null)
            cameraStandHeight = playerCamera.transform.localPosition.y;

        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook();
        HandleCrouch();
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

    void HandleCrouch()
    {
        if (Input.GetKey(crouchKey))
        {
            // 按下按键：切换为下蹲状态
            if (!isCrouching)
            {
                isCrouching = true;
                controller.height = crouchHeight;
                controller.center = new Vector3(0, crouchHeight * 0.5f, 0);
                SetCameraHeight(cameraCrouchHeight);
            }
        }
        else
        {
            // 松开按键：尝试站起
            if (isCrouching && CanStand())
            {
                isCrouching = false;
                controller.height = standingHeight;
                controller.center = new Vector3(0, standingHeight * 0.5f, 0);
                SetCameraHeight(cameraStandHeight);
            }
        }
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

        // 速度计算：下蹲 > 奔跑 > 行走
        float currentSpeed;
        if (isCrouching)
        {
            currentSpeed = crouchSpeed * currentSpeedMultiplier;
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            currentSpeed = runSpeed * currentSpeedMultiplier;
        }
        else
        {
            currentSpeed = walkSpeed * currentSpeedMultiplier;
        }

        controller.Move(move * currentSpeed * Time.deltaTime);

        // 跳跃逻辑（含下蹲自动站起）
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            if (isCrouching)
            {
                // 尝试先站起再跳跃
                if (CanStand())
                {
                    isCrouching = false;
                    controller.height = standingHeight;
                    controller.center = new Vector3(0, standingHeight * 0.5f, 0);
                    SetCameraHeight(cameraStandHeight);
                    verticalVelocity = Mathf.Sqrt(jumpHeight * currentJumpMultiplier * -2f * gravity);
                }
                // 头顶有障碍物，无法站起，忽略跳跃
            }
            else
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * currentJumpMultiplier * -2f * gravity);
            }
        }

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

    /// <summary> 检测头顶是否有足够空间站起 </summary>
    private bool CanStand()
    {
        float radius = controller.radius;
        float crouchTop = transform.position.y + crouchHeight;
        float standingTop = transform.position.y + standingHeight;

        // 检查从下蹲顶部到站立顶部的胶囊体区域
        Vector3 bottomSphere = new Vector3(transform.position.x, crouchTop + radius * 0.5f, transform.position.z);
        Vector3 topSphere = new Vector3(transform.position.x, standingTop - radius * 0.5f, transform.position.z);
        if (bottomSphere.y > topSphere.y)
            bottomSphere.y = topSphere.y;

        // 排除自身所在层，避免检测到自己的碰撞体
        int layerMask = ~(1 << gameObject.layer);
        return !Physics.CheckCapsule(bottomSphere, topSphere, radius, layerMask, QueryTriggerInteraction.Ignore);
    }

    private void SetCameraHeight(float height)
    {
        if (playerCamera != null)
        {
            Vector3 pos = playerCamera.transform.localPosition;
            pos.y = height;
            playerCamera.transform.localPosition = pos;
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