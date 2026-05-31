using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
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

    // Private variables
    private CharacterController controller;
    private Vector3 moveDirection;
    private float verticalRotation = 0f;
    private float currentSpeed;
    private float targetSpeed;
    private bool isGrounded;
    private float verticalVelocity;

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
    }

    private void HandleMouseLook()
    {
        // If sun controller is active (right mouse held), don't move camera
        if (SunController.IsControllingSun)
            return;

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