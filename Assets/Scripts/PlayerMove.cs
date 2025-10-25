using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMove : MonoBehaviour
{
    [Header("Movement")] public float moveSpeed = 6f;
    public float runMultiplier = 1.8f;
    public float jumpHeight = 1.5f;
    public float gravity = -18f;

    [Header("Mouse Look")] public float mouseSensitivity = 0.6f;
    public Transform cameraTransform;
    public bool lockCursor = true;

    [Header("Ground Check")] public Transform optionalGroundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundMask;
    public float extraGroundCheckDistance = 0.05f;

    [Header("Camera Bob")] public bool bobEnabled = true;
    public float bobSpeed = 10f;           // частота при полном сприне
    public float bobAmount = 0.1f;        // вертикальная амплитуда
    public float bobSway = 0.06f;          // горизонтальная амплитуда (легкий крен)
    public float bobTransitionSpeed = 8f;  // скорость нарастания эффекта

    private CharacterController cc;
    private Vector3 velocity;
    private float xRotation = 0f;

    // bob state
    private float bobTimer = 0f;
    private float bobAmountCurrent = 0f; // 0..1 — сила эффекта
    private Vector3 cameraOriginalLocalPos;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        if (cc == null) cc = gameObject.AddComponent<CharacterController>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (cameraTransform != null)
            cameraOriginalLocalPos = cameraTransform.localPosition;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            // Cursor.visible = false;
            Cursor.visible = true;
           
        }
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
        HandleCameraBob(moveInput, run);
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
                if (gp.sqrMagnitude > 0.001f) return gp;
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

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

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

        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        if (jumpPressed && grounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }

    void HandleCameraBob(Vector2 moveInput, bool isRunning)
    {
        if (!bobEnabled || cameraTransform == null) return;

        // сила эффекта: зависит от скорости движения и оттого, бежим ли мы
        float moveMagnitude = new Vector2(moveInput.x, moveInput.y).magnitude;
        float targetBobStrength = (isRunning ? 1f : 0.4f) * moveMagnitude;

        // плавно интерполируем силу эффекта
        bobAmountCurrent = Mathf.Lerp(bobAmountCurrent, targetBobStrength, Time.deltaTime * bobTransitionSpeed);

        // увеличиваем таймер только если есть сила
        if (bobAmountCurrent > 0.001f)
            bobTimer += Time.deltaTime * bobSpeed;
        else
            bobTimer = 0f;

        // синусоидальные смещения
        float bobOffsetY = Mathf.Sin(bobTimer * 2f) * bobAmount * bobAmountCurrent;
        float bobOffsetX = Mathf.Sin(bobTimer) * bobSway * bobAmountCurrent;
        float bobTilt = Mathf.Sin(bobTimer) * bobSway * 5f * bobAmountCurrent; // небольшой крен

        // комбинируем исходную позицию камеры с эффектом
        cameraTransform.localPosition = cameraOriginalLocalPos + new Vector3(bobOffsetX, bobOffsetY, 0f);

        // легкий крен камеры по локальной Z для ощущения наклона
        cameraTransform.localRotation = Quaternion.Euler(cameraTransform.localRotation.eulerAngles.x, cameraTransform.localRotation.eulerAngles.y, bobTilt);
    }

    void OnDrawGizmosSelected()
    {
        if (optionalGroundCheck != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(optionalGroundCheck.position, groundCheckRadius);
        }
        else if (cc != null)
        {
            Vector3 bottomCenter = transform.position + Vector3.up * (cc.center.y - cc.height / 2f);
            Vector3 checkPos = bottomCenter + Vector3.up * (groundCheckRadius + extraGroundCheckDistance);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(checkPos + Vector3.down * (groundCheckRadius + extraGroundCheckDistance * 0.5f), groundCheckRadius);
        }
    }

    void OnGUI()
    {
        GUI.Label(new Rect(10, 10, 500, 20), "Grounded: " + IsGrounded());
        GUI.Label(new Rect(10, 30, 500, 20), "VelocityY: " + velocity.y.ToString("F2"));
    }
    
    private void RespawnPlayer(Transform respawnAt)
    {
        return;
    }
    
}