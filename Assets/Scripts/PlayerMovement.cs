using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private float groundCheckOffset = 0.1f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Mouse Look")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private WeaponHolder weaponHolder;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch;
    private bool isGrounded;
    private float lastTimeGrounded;
    private float lastTimeJumpPressed;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (weaponHolder == null) weaponHolder = GetComponentInChildren<WeaponHolder>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleLook();
        UpdateGrounded();
        HandleMovement();
    }

    private void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        if (weaponHolder != null)
        {
            Vector2 recoil = weaponHolder.ConsumeRecoilDelta();
            cameraPitch += recoil.x;
            transform.Rotate(Vector3.up * recoil.y);
        }
        cameraPitch = Mathf.Clamp(cameraPitch, minLookAngle, maxLookAngle);

        if (playerCamera != null)
        {
            playerCamera.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }

    private void HandleMovement()
    {
        if (isGrounded && velocity.y < 0f)
        {
            velocity.y = -2f;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (Input.GetButtonDown("Jump"))
        {
            lastTimeJumpPressed = Time.time;
        }

        bool canCoyote = Time.time - lastTimeGrounded <= coyoteTime;
        bool buffered = Time.time - lastTimeJumpPressed <= jumpBufferTime;
        if (buffered && (isGrounded || canCoyote))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastTimeJumpPressed = -999f; // consume buffer
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    private void UpdateGrounded()
    {
        // Robust grounded detection using sphere cast slightly below the controller
        Vector3 origin = transform.position + Vector3.up * groundCheckOffset;
        float radius = groundCheckRadius;
        float castDist = groundCheckOffset + 0.2f;
        bool hit = Physics.SphereCast(origin, radius, Vector3.down, out RaycastHit hitInfo, castDist, groundMask, QueryTriggerInteraction.Ignore);
        isGrounded = hit;
        if (isGrounded) lastTimeGrounded = Time.time;
    }
}
