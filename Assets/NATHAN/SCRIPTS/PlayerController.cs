using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 10f;

    [Header("Acceleration")]
    [SerializeField] private float groundAcceleration = 5f;   // slower = more sluggish
    [SerializeField] private float groundDeceleration = 8f;   // slower to stop
    [SerializeField] private float airControl = 0.3f;        // reduced steering in air
    [SerializeField] private float sprintAccelMultiplier = 1.2f; // extra oomph when sprinting

    [Header("Jump & Gravity")]
    [SerializeField] private float gravity = -18f;            // heavier gravity for cyclops
    [SerializeField] private float jumpHeight = 1.2f;         // lower jump

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 1.5f;   // slower turning
    [SerializeField] private float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;

    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalRotation = 0f;
    private float currentSpeed;
    private float targetSpeed;
    private bool isGrounded;
    private float verticalVelocity;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovementInput();
        ApplyGravityAndJump();
        ApplyMovement();
    }

    void HandleMouseLook()
    {
        if (SunController.IsControllingSun) return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovementInput()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 worldDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;

        // Target speed based on walk/sprint
        float baseTargetSpeed = isSprinting ? sprintSpeed : walkSpeed;
        targetSpeed = baseTargetSpeed * inputDirection.magnitude;

        // Different acceleration for grounded vs air
        float currentAccel = isGrounded ? groundAcceleration : groundAcceleration * airControl;
        if (isSprinting && isGrounded)
            currentAccel *= sprintAccelMultiplier;

        float accelRate = (inputDirection.magnitude > 0) ? currentAccel : groundDeceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, accelRate * Time.deltaTime);

        moveDirection = worldDirection * currentSpeed;
    }

    void ApplyGravityAndJump()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;
    }

    void ApplyMovement()
    {
        Vector3 finalMovement = moveDirection + Vector3.up * verticalVelocity;
        controller.Move(finalMovement * Time.deltaTime);
    }

    void OnApplicationFocus(bool hasFocus)
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