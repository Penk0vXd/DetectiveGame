using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(PlayerInputReader))]
public class FirstPersonController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gravity = -9.81f;

    private CharacterController controller;
    private Camera playerCamera;
    private PlayerInputReader inputReader;
    private float cameraPitch;
    private float verticalVelocity;
    private bool controlEnabled = true;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerCamera = GetComponentInChildren<Camera>();
        inputReader = GetComponent<PlayerInputReader>();

        if (playerCamera == null)
        {
            Debug.LogError("FirstPersonController не намери Camera в Player", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // заключва курсора
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (!controlEnabled)
        {
            return;
        }

        // върти играча и камерата
        Vector2 lookInput = inputReader.ReadLook();
        float mouseX = lookInput.x * mouseSensitivity;
        float mouseY = lookInput.y * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -80f, 80f);

        playerCamera.transform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        // прилага гравитация
        if (controller.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        // движи играча
        Vector2 moveInput = inputReader.ReadMove();
        Vector3 moveDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        controller.Move(velocity * Time.deltaTime);
    }

    public void SetControlEnabled(bool isEnabled)
    {
        // включва или спира управлението
        controlEnabled = isEnabled;
    }
}
