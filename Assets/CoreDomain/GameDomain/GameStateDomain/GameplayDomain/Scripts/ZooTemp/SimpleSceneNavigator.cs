using UnityEngine;

public class SimpleSceneNavigator : MonoBehaviour {
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float speedStep = 5f;
    [SerializeField] private float minSpeed = 1f;
    [SerializeField] private float maxSpeed = 100f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 89f;

    private float _verticalRotation;

    private void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _verticalRotation = transform.localEulerAngles.x;

        if (_verticalRotation > 180f)
            _verticalRotation -= 360f;
    }

    private void Update() {
        HandleMovement();
        HandleHeight();
        HandleSpeed();
        HandleMouseLook();
    }

    private void HandleMovement() {
        float horizontal = 0f;
        float vertical = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal += 1f;

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            vertical += 1f;

        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            vertical -= 1f;

        Vector3 direction =
            transform.forward * vertical +
            transform.right * horizontal;

        if (direction.sqrMagnitude > 0f) {
            transform.position +=
                direction.normalized * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleHeight() {
        if (Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift)) {
            transform.position +=
                Vector3.up * moveSpeed * Time.deltaTime;
        }

        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl)) {
            transform.position +=
                Vector3.down * moveSpeed * Time.deltaTime;
        }
    }

    private void HandleSpeed() {
        if (Input.GetKeyDown(KeyCode.Q)) {
            moveSpeed = Mathf.Min(
                moveSpeed + speedStep,
                maxSpeed
            );
        }

        if (Input.GetKeyDown(KeyCode.E)) {
            moveSpeed = Mathf.Max(
                moveSpeed - speedStep,
                minSpeed
            );
        }
    }

    private void HandleMouseLook() {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(
            Vector3.up * mouseX,
            Space.World
        );

        _verticalRotation -= mouseY;

        _verticalRotation = Mathf.Clamp(
            _verticalRotation,
            -maxLookAngle,
            maxLookAngle
        );

        Vector3 currentRotation = transform.eulerAngles;

        transform.rotation = Quaternion.Euler(
            _verticalRotation,
            currentRotation.y,
            0f
        );
    }

    private void OnDisable() {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}