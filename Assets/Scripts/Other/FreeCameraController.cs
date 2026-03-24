using UnityEngine;
using UnityEngine.InputSystem;

public class FreeCameraController : MonoBehaviour
{
    public float lookSpeed = 3f;
    public float moveSpeed = 5f;

    [SerializeField] public float rotationSensitivity = 1f;
    [SerializeField] public float movementSensitivity = 1f; 

    public float fastMultiplier = 2f;
    public float slowMultiplier = 0.5f;

    private float rotationX = 0f;
    private float rotationY = 0f;

    void Start()
    {
        Vector3 angles = transform.localEulerAngles;
        rotationX = angles.x;
        rotationY = angles.y;
    }

    void Update()
    {
        if (Mouse.current.rightButton.isPressed)
        {
            HandleMouseLook();
            HandleMovement();
        }
    }

    void HandleMouseLook()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * lookSpeed * rotationSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * lookSpeed * rotationSensitivity * Time.deltaTime;

        rotationY += mouseX;
        rotationX -= mouseY;

        rotationX = Mathf.Clamp(rotationX, -90f, 90f);

        transform.localRotation = Quaternion.Euler(rotationX, rotationY, 0f);
    }

    void HandleMovement()
    {
        float moveX = 0f;
        float moveZ = 0f;

        if (Keyboard.current.wKey.isPressed) moveZ += 1f;
        if (Keyboard.current.sKey.isPressed) moveZ -= 1f;
        if (Keyboard.current.dKey.isPressed) moveX += 1f;
        if (Keyboard.current.aKey.isPressed) moveX -= 1f;

        float speed = moveSpeed * movementSensitivity;

        if (Keyboard.current.leftShiftKey.isPressed)
            speed *= fastMultiplier;
        else if (Keyboard.current.leftCtrlKey.isPressed)
            speed *= slowMultiplier;

        Vector3 move = transform.right * moveX + transform.forward * moveZ;

        transform.position += move * speed * Time.deltaTime;
    }
}