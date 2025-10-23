using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Настройки движения")]
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    [Header("Настройки камеры")]
    [SerializeField] private Transform cameraHolder; // Объект-держатель камеры
    private Camera playerCamera;

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

    void Awake()
    {
        // Настройка камеры
        SetupCamera();
    }

    void Start()
    {
        // Блокировка и скрытие курсора
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void SetupCamera()
    {
        // Если держатель камеры не назначен, ищем или создаем его
        if (cameraHolder == null)
        {
            // Проверяем, есть ли дочерний объект CameraHolder
            cameraHolder = transform.Find("CameraHolder");
            
            // Если нет, создаем новый
            if (cameraHolder == null)
            {
                GameObject holderObj = new GameObject("CameraHolder");
                cameraHolder = holderObj.transform;
                cameraHolder.parent = transform;
                cameraHolder.localPosition = new Vector3(0, 1.6f, 0); // Примерная высота глаз
                cameraHolder.localRotation = Quaternion.identity;
            }
        }

        // Проверяем/настраиваем камеру
        playerCamera = cameraHolder.GetComponentInChildren<Camera>();
        if (playerCamera == null)
        {
            // Если камера не найдена в дочерних объектах, ищем главную камеру
            playerCamera = Camera.main;
            if (playerCamera != null)
            {
                // Перемещаем главную камеру как дочерний объект
                playerCamera.transform.parent = cameraHolder;
                playerCamera.transform.localPosition = Vector3.zero;
                playerCamera.transform.localRotation = Quaternion.identity;
            }
            else
            {
                // Если камера не найдена вообще, создаем новую
                GameObject cameraObj = new GameObject("PlayerCamera");
                playerCamera = cameraObj.AddComponent<Camera>();
                cameraObj.transform.parent = cameraHolder;
                cameraObj.transform.localPosition = Vector3.zero;
                cameraObj.transform.localRotation = Quaternion.identity;
                Debug.Log("Создана новая камера игрока");
            }
        }
    }

    void Update()
    {
        if (cameraHolder == null)
        {
            Debug.LogError("CameraHolder не найден!");
            return;
        }

        // Движение
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        transform.position += move * moveSpeed * Time.deltaTime;

        // Вращение камеры
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        cameraHolder.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }
}