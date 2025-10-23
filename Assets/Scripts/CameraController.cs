using UnityEngine;

public class MouseLook : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;

    private Vector2 moveInput;
    private Vector2 lookInput;
    private float xRotation = 0f;

    public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    public void OnLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        lookInput = context.ReadValue<Vector2>();
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Движение
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Вращение мышью
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}