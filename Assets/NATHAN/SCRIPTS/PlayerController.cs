using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float acceleration = 10f;    // How fast you reach max speed
    [SerializeField] private float deceleration = 10f;    // How fast you stop
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("References")]
    [SerializeField] private Camera playerCamera;          // Assign in inspector or will find

    // Private variables
    private CharacterController controller;
    private Vector3 moveDirection;
    private Vector3 currentVelocity;
    private float verticalRotation = 0f;
    private float currentSpeed;
    private float targetSpeed;
    private bool isGrounded;
    private float verticalVelocity;

    private void Start()
    {
        controller = GetComponent<CharacterController>();

        // If camera not assigned, try to find it in children
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();

        // Lock cursor to center of screen and hide it
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleMovementInput();
        ApplyGravityAndJump();
        ApplyMovement();
    }

    private void HandleMouseLook()
    {
        // Get mouse input
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Horizontal rotation (turns whole player)
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation (only camera)
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    private void HandleMovementInput()
    {
        // Get input axes
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        // Calculate target direction relative to player's orientation
        Vector3 inputDirection = new Vector3(horizontal, 0f, vertical).normalized;
        Vector3 worldDirection = transform.right * inputDirection.x + transform.forward * inputDirection.z;

        // Determine target speed (walk or sprint)
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && inputDirection.magnitude > 0;
        targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // Smoothly change current speed (acceleration/deceleration)
        float accelRate = (inputDirection.magnitude > 0) ? acceleration : deceleration;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed * inputDirection.magnitude, accelRate * Time.deltaTime);

        // Apply direction * current speed to moveDirection
        moveDirection = worldDirection * currentSpeed;
    }

    private void ApplyGravityAndJump()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // Small downward force to stick to ground

        // Jump input
        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // Apply gravity
        verticalVelocity += gravity * Time.deltaTime;
    }

    private void ApplyMovement()
    {
        // Combine horizontal movement and vertical velocity
        Vector3 finalMovement = moveDirection + Vector3.up * verticalVelocity;

        // Move the character controller
        controller.Move(finalMovement * Time.deltaTime);
    }

    // Optional: Unlock cursor when pressing Escape (good for debugging)
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