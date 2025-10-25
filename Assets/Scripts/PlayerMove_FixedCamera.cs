using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class PlayerMove_FixedCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float runMultiplier = 1.8f;
    public float jumpHeight = 1.5f;
    public float gravity = -18f;

    [Header("Look")]
    public float mouseSensitivity = 0.6f;
    public Transform cameraTransform;
    public bool lockCursor = true;
    public bool hideCursor = true;

    [Header("Ground Check")]
    public Transform optionalGroundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;
    public float extraGroundCheckDistance = 0.05f;

    [Header("Camera Bob (optional, harmless)")]
    public bool bobEnabled = true;
    public float bobSpeed = 10f;
    public float bobAmount = 0.1f;
    public float bobSway = 0.06f;
    public float bobTransitionSpeed = 8f;

    CharacterController cc;
    Vector3 velocity;
    float xRotation = 0f;

    // camera / bob state
    Vector3 cameraOriginalLocalPos;
    float bobTimer = 0f;
    float bobAmountCurrent = 0f;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Start()
    {
        // Ensure camera reference
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;

        // Force CharacterController to use its current height as "standing" and ensure center = height/2
        float minH = cc.radius * 2f + 0.01f;
        cc.height = Mathf.Max(cc.height, minH);
        cc.center = new Vector3(cc.center.x, cc.height / 2f, cc.center.z);

        // Cache original camera local position after controller is set
        if (cameraTransform != null) cameraOriginalLocalPos = cameraTransform.localPosition;

        // Lock cursor if required
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = !hideCursor;
        }

        // Make sure camera is exactly at cached local position
        if (cameraTransform != null) cameraTransform.localPosition = cameraOriginalLocalPos;
    }

    void Update()
    {
        Vector2 moveInput = ReadMoveInput();
        Vector2 lookInput = ReadLookInput();
        bool run = ReadRunInput();
        bool jump = ReadJumpInput();

        HandleLook(lookInput);
        HandleMove(moveInput, run);
        HandleJumpAndGravity(jump);
        ApplyCameraBob(moveInput, run);

        // Safety: enforce camera local position every frame so nothing else can lower it
        if (cameraTransform != null && cameraTransform.localPosition.y < cameraOriginalLocalPos.y)
            cameraTransform.localPosition = new Vector3(cameraOriginalLocalPos.x, cameraOriginalLocalPos.y, cameraOriginalLocalPos.z);

        // Safety: enforce CC center/height consistency (prevents external scripts from collapsing controller)
        float minHeight = cc.radius * 2f + 0.01f;
        if (cc.height < minHeight) cc.height = minHeight;
        cc.center = new Vector3(cc.center.x, cc.height / 2f, cc.center.z);
    }

    Vector2 ReadMoveInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float x = 0f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x = -1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x = 1f;
            float y = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) y = 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) y = -1f;
            Vector2 k = new Vector2(x, y);
            if (Gamepad.current != null)
            {
                Vector2 gp = Gamepad.current.leftStick.ReadValue();
                if (gp.sqrMagnitude > 0.001f) return gp.normalized;
            }
            return k.normalized;
        }
#endif
        return new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    Vector2 ReadLookInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            if (Gamepad.current != null)
            {
                Vector2 gp = Gamepad.current.rightStick.ReadValue();
                if (gp.sqrMagnitude > 0.001f) return gp;
            }
            return delta;
        }
#endif
        return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    }

    bool ReadRunInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null) return Keyboard.current.leftShiftKey.isPressed;
#endif
        return Input.GetKey(KeyCode.LeftShift);
    }

    bool ReadJumpInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame) return true;
            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame) return true;
            return false;
        }
#endif
        return Input.GetKeyDown(KeyCode.Space);
    }

    void HandleMove(Vector2 input, bool run)
    {
        float speed = moveSpeed * (run ? runMultiplier : 1f);
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        cc.Move(move * speed * Time.deltaTime);
    }

    void HandleLook(Vector2 look)
    {
        float mouseX = look.x * mouseSensitivity * Time.deltaTime;
        float mouseY = look.y * mouseSensitivity * Time.deltaTime;

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.delta != null)
        {
            mouseX = look.x * mouseSensitivity;
            mouseY = look.y * mouseSensitivity;
        }
#endif

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (cameraTransform != null) cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    bool IsGrounded()
    {
        if (optionalGroundCheck != null)
        {
            Collider[] hits = Physics.OverlapSphere(optionalGroundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < hits.Length; i++)
            {
                Collider c = hits[i];
                if (c == null) continue;
                if (c.transform.IsChildOf(transform)) continue;
                return true;
            }
            return cc.isGrounded;
        }

        Vector3 bottomCenter = transform.position + Vector3.up * (cc.center.y - cc.height / 2f);
        Vector3 checkPos = bottomCenter + Vector3.up * (groundCheckRadius + extraGroundCheckDistance);
        if (Physics.SphereCast(checkPos, groundCheckRadius, Vector3.down, out RaycastHit hit, groundCheckRadius + extraGroundCheckDistance, groundMask, QueryTriggerInteraction.Ignore))
            return true;
        return cc.isGrounded;
    }

    void HandleJumpAndGravity(bool jumpPressed)
    {
        bool grounded = IsGrounded();
        if (grounded && velocity.y < 0f) velocity.y = -2f;
        if (jumpPressed && grounded) velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void ApplyCameraBob(Vector2 moveInput, bool isRunning)
    {
        if (!bobEnabled || cameraTransform == null) return;

        float moveMagnitude = new Vector2(moveInput.x, moveInput.y).magnitude;
        float targetBobStrength = (isRunning ? 1f : 0.4f) * moveMagnitude;
        bobAmountCurrent = Mathf.Lerp(bobAmountCurrent, targetBobStrength, Time.deltaTime * bobTransitionSpeed);

        if (bobAmountCurrent > 0.001f) bobTimer += Time.deltaTime * bobSpeed; else bobTimer = 0f;

        float bobOffsetY = Mathf.Sin(bobTimer * 2f) * bobAmount * bobAmountCurrent;
        float bobOffsetX = Mathf.Sin(bobTimer) * bobSway * bobAmountCurrent;
        float bobTilt = Mathf.Sin(bobTimer) * bobSway * 5f * bobAmountCurrent;

        // apply bob relative to original (but never lower than original Y due to safety check in Update)
        cameraTransform.localPosition = cameraOriginalLocalPos + new Vector3(bobOffsetX, bobOffsetY, 0f);
        cameraTransform.localRotation = Quaternion.Euler(cameraTransform.localRotation.eulerAngles.x, cameraTransform.localRotation.eulerAngles.y, bobTilt);
    }

    void OnGUI()
    {
        if (cameraTransform != null)
        {
            GUI.Label(new Rect(10, 10, 400, 20), "Cam LocalY: " + cameraTransform.localPosition.y.ToString("F3"));
        }
        if (cc != null)
        {
            GUI.Label(new Rect(10, 30, 400, 20), "CC Height: " + cc.height.ToString("F3") + "  CenterY: " + cc.center.y.ToString("F3"));
        }
    }
}