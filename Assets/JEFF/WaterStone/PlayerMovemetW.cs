using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementW : MonoBehaviour
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

    [Header("Water Wave Settings")]
    [SerializeField] private GameObject waterWavePrefab;  // 水波预制体
    [SerializeField] private float waveCooldown = 0.5f;    // 冷却时间（秒）

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    // Private variables
    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 currentVelocity;
    private float verticalRotation = 0f;
    private float currentSpeed;
    private float targetSpeed;
    private bool isGrounded;
    private float verticalVelocity;
    private float lastWaveTime = -999f;  // 上次生成水波的时间

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovementInput();
        ApplyGravityAndJump();
        ApplyMovement();
        HandleWaterWave();  // 新增：处理水波生成
    }

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

    private void ApplyGravityAndJump()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        Vector3 finalMovement = moveDirection + Vector3.up * verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);
    }

    private void HandleWaterWave()
    {
        // 按下E键且冷却时间已过
        if (Input.GetKeyDown(KeyCode.E) && Time.time - lastWaveTime >= waveCooldown)
        {
            SpawnWaterWave();
            lastWaveTime = Time.time;
        }
    }

    private void SpawnWaterWave()
    {
        if (waterWavePrefab == null)
        {
            Debug.LogWarning("Water Wave Prefab 未设置！");
            return;
        }

        // 在玩家脚下生成水波（Y轴稍微偏移避免穿模）
        Vector3 spawnPosition = transform.position;
        spawnPosition.y = 0.05f;  // 略高于地面

        Instantiate(waterWavePrefab, spawnPosition, Quaternion.identity);
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