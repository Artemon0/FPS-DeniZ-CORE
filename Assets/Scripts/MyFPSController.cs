using UnityEngine;

public class My_First_Person_Movement : MonoBehaviour
{
    private Vector3 Velocity;
    private Vector3 PlayerMovementInput;
    private Vector2 PlayerMouseInput;
    private bool Sneaking = false;
    private float xRotation;

    [Header("Components Needed")] [SerializeField]
    private Transform PlayerCamera;

    [SerializeField] private CharacterController Controller;
    [SerializeField] private Transform Player;

    [Space] [Header("Movement")] [SerializeField]
    private float Speed;

    [SerializeField] private float JumpForce;
    [SerializeField] private float Sensetivity;
    [SerializeField] private float Gravity = 9.81f;

    private double timer = 0.0;
    private bool isDied = false;
    private Vector3 respawnAt = new Vector3(-23.41f, 0f, -4.81f);

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        PlayerMovementInput = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        PlayerMouseInput = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        MovePlayer();
        MoveCamera();


        if (Input.GetKeyUp(KeyCode.RightShift))
        {
            Player.localScale = new Vector3(1f, 1f, 1f);
            Sneaking = false;
        }


        if (GetPlayerPosition().y < -10)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(0);
        }

        timer += Time.deltaTime;

        if (isRespawnButtonPressed(KeyCode.R) && timer > 2.0)
        {
            isDied = true;
        }


        if (isDied)
        {
            Die();
        }
    }

    private void MovePlayer()
    {
        Vector3 MoveVector = transform.TransformDirection(PlayerMovementInput);


        if (Controller.isGrounded)
        {
            Velocity.y = -1f;

            if (Input.GetKeyDown(KeyCode.Space) && Sneaking == false)
            {
                Velocity.y = JumpForce;
            }
        }
        else
        {
            Velocity.y += Gravity * -2f * Time.deltaTime;
        }


        {
            Controller.Move(MoveVector * Speed * Time.deltaTime);
        }

        Controller.Move(Velocity * Time.deltaTime);
    }

    private void MoveCamera()
    {
        xRotation -= PlayerMouseInput.y * Sensetivity;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        transform.Rotate(0f, PlayerMouseInput.x * Sensetivity, 0f);
        PlayerCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    private Vector3 GetPlayerPosition()
    {
        return Player.transform.position;
    }

    private void SetPlayerPosition(Vector3 newPosition)
    {
        Player.transform.position = newPosition;
    }

    // I need to respawn the player at a specific position
    private void RespawnPlayer() // respawn at new scene with timer
    {
        return;
    }

    private bool isRespawnButtonPressed(KeyCode key)
    {
        return Input.GetKey(key);
    }

    void Die()
    {
        return;
    }
}