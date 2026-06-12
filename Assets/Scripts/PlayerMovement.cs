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

    [Header("Ladder")]
    [SerializeField] private LayerMask ladderMask;
    [SerializeField] private float ladderCheckRadius = 0.35f;
    [SerializeField] private float climbSpeed = 3f;

    [Header("Mouse Look")]
    [SerializeField] private Transform playerCamera;
    [SerializeField] private WeaponHolder weaponHolder;
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Crouch")]
    [SerializeField] private float crouchSpeedMultiplier = 0.5f;
    [SerializeField] private float crouchHeight = 1.0f;
    [SerializeField] private float crouchCameraOffsetY = -0.5f;
    [SerializeField] private float crouchTransitionSpeed = 10f;

    private CharacterController characterController;
    private Vector3 velocity;
    private float cameraPitch;
    private bool isGrounded;
    private float lastTimeGrounded;
    private float lastTimeJumpPressed;
    private bool onLadder;
    private bool isCrouching;
    private float defaultHeight;
    private Vector3 defaultCenter;
    private Vector3 camDefaultLocalPos;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (weaponHolder == null) weaponHolder = GetComponentInChildren<WeaponHolder>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        defaultHeight = characterController.height;
        defaultCenter = characterController.center;
        if (playerCamera != null) camDefaultLocalPos = playerCamera.localPosition;
    }

    private void Update()
    {
        // Блокируем управление в паузе
        if (PauseMenu.IsPaused)
            return;

        HandleLook();
        UpdateGrounded();
        UpdateLadder();
        HandleCrouch();
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
        if (!onLadder)
        {
            if (isGrounded && velocity.y < 0f)
            {
                velocity.y = -2f;
            }
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && !isCrouching;

        Vector3 moveDirection = transform.right * horizontal + transform.forward * vertical;
        moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);

        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;
        if (isCrouching) currentSpeed *= crouchSpeedMultiplier;
        characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        if (!onLadder)
        {
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
        }
        else
        {
            // ladder climb: vertical input drives Y, no gravity
            velocity.y = vertical * climbSpeed;
        }

        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl);
        if (wantCrouch)
        {
            isCrouching = true;
        }
        else
        {
            if (CanStandUp()) isCrouching = false;
        }

        float targetHeight = isCrouching ? Mathf.Max(0.8f, crouchHeight) : defaultHeight;
        float newHeight = Mathf.Lerp(characterController.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        float delta = defaultHeight - newHeight;
        Vector3 newCenter = defaultCenter + new Vector3(0f, -delta * 0.5f, 0f);
        characterController.center = newCenter;
        characterController.height = newHeight;

        if (playerCamera != null)
        {
            Vector3 targetCam = camDefaultLocalPos + new Vector3(0f, isCrouching ? crouchCameraOffsetY : 0f, 0f);
            playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetCam, Time.deltaTime * crouchTransitionSpeed);
        }
    }

    private bool CanStandUp()
    {
        float standHeight = defaultHeight;
        float currentHeight = characterController.height;
        if (standHeight <= currentHeight + 0.01f) return true;
        float extra = (standHeight - currentHeight) + 0.05f;
        Vector3 origin = transform.position + Vector3.up * (characterController.center.y + currentHeight * 0.5f);
        float radius = characterController.radius * 0.95f;
        bool blocked = Physics.SphereCast(origin, radius, Vector3.up, out _, extra, ~0, QueryTriggerInteraction.Ignore);
        return !blocked;
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

    private void UpdateLadder()
    {
        // Check small sphere to see if we're inside ladder volume
        onLadder = Physics.CheckSphere(transform.position, ladderCheckRadius, ladderMask, QueryTriggerInteraction.Collide);
        if (onLadder)
        {
            // reset vertical velocity to prevent gravity pull when entering
            velocity.y = 0f;
        }
    }
}
