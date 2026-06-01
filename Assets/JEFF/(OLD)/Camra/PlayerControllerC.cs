using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerControllerC : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    [Header("Telescope & Teleport")]
    [SerializeField] private float zoomSpeed = 30f;            // 缩放灵敏度（滚轮/左右键）
    [SerializeField] private float minFOV = 10f;               // 最小视野（最大放大）
    [SerializeField] private float maxFOV = 90f;               // 最大视野（最大缩小）
    [SerializeField] private float maxTeleportDistance = 200f; // 最大传送距离
    [SerializeField] private LayerMask teleportMask = ~0;      // 射线检测层（默认全部）

    // 私有变量
    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 currentVelocity;
    private float verticalRotation = 0f;
    private float currentSpeed;
    private float targetSpeed;
    private bool isGrounded;
    private float verticalVelocity;

    private float defaultFOV;      // 进入游戏时的初始 FOV，用于判断是否处于放大状态

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // 记录默认视野
        if (playerCamera != null)
            defaultFOV = playerCamera.fieldOfView;
        else
            defaultFOV = 60f; // 后备值

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleZoom();               // 处理滚轮和左右键缩放
        HandleMovementInput();
        ApplyGravityAndJump();      // 内部已包含传送逻辑
        ApplyMovement();
    }

    // ---------- 缩放控制 ----------
    private void HandleZoom()
    {
        if (playerCamera == null) return;

        float targetFOV = playerCamera.fieldOfView;

        // 鼠标滚轮
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
        {
            targetFOV -= scroll * zoomSpeed;
        }

        // 鼠标左键 - 放大（减小 FOV）
        if (Input.GetMouseButton(0))
        {
            targetFOV -= zoomSpeed * Time.deltaTime;
        }

        // 鼠标右键 - 缩小（增大 FOV）
        if (Input.GetMouseButton(1))
        {
            targetFOV += zoomSpeed * Time.deltaTime;
        }

        // 限制范围并应用
        targetFOV = Mathf.Clamp(targetFOV, minFOV, maxFOV);
        playerCamera.fieldOfView = targetFOV;
    }

    // ---------- 传送（替代跳跃，仅在放大状态下） ----------
    private void ApplyGravityAndJump()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        // 判断当前是否处于望远镜放大状态（FOV 小于默认值）
        bool isZoomedIn = playerCamera != null && playerCamera.fieldOfView < defaultFOV - 0.1f;

        // 如果放大 + 按下 Space → 传送到视线目标
        if (isZoomedIn && Input.GetButtonDown("Jump"))
        {
            TeleportToLookTarget();
            // 传送后可选：重置 FOV 或保持，这里重置为默认
            playerCamera.fieldOfView = defaultFOV;
            return; // 不进行普通跳跃
        }

        // 普通跳跃（仅在未放大时生效）
        if (isGrounded && Input.GetButtonDown("Jump"))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void TeleportToLookTarget()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxTeleportDistance, teleportMask))
        {
            // 目标点：射线碰撞点
            Vector3 targetPoint = hit.point;

            // 简单安全处理：将角色放在碰撞点上方，避免穿入地面
            float playerHeight = controller.height;
            float skinWidth = controller.skinWidth;
            Vector3 teleportPos = targetPoint + Vector3.up * (playerHeight * 0.5f + skinWidth);

            // 禁用角色控制器，直接设置位置，再启用
            controller.enabled = false;
            transform.position = teleportPos;
            controller.enabled = true;

            // 可选：保持水平旋转不变，或看向目标点方向
            // Vector3 lookDir = hit.normal; 等
        }
        else
        {
            Debug.Log("传送失败：视线内无有效目标");
        }
    }

    // ---------- 原有移动、视角（未改动核心） ----------
    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 worldDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;
        targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        float accelRate = (inputDirection.magnitude > 0) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed * inputDirection.magnitude, accelRate * Time.deltaTime);

        moveDirection = worldDirection * currentSpeed;
    }

    private void ApplyMovement()
    {
        Vector3 finalMovement = moveDirection + Vector3.up * verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (hasFocus && Application.isFocused)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}