using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class FpsLocalInput : MonoBehaviour
{
    [Header("Network")]
    public int inputSendRate = 30;

    private float sendAccumulator;
    private float accumulatedLookX;
    private float accumulatedLookY;
    private bool jumpPending;
    private bool firePending;
    private int inputSequence;
    private int previousPlayerCount;

    void Start()
    {
        ShowCursor();
        previousPlayerCount = 0;
    }

    void Update()
    {
        TcpClient client = TcpClient.Instance;

        if (client == null || !client.Connected || client.LocalPlayerId < 0)
        {
            ShowCursor();
            previousPlayerCount = 0;
            return;
        }

        int playerCount = client.PlayerCount;

        if (playerCount <= 1)
        {
            ShowCursor();
            previousPlayerCount = playerCount;
            return;
        }

        if (previousPlayerCount <= 1 && playerCount > 1)
        {
            HideCursor();
        }

        previousPlayerCount = playerCount;

        if (WasEscapePressed())
        {
            ShowCursor();
            client.SendInput(++inputSequence, 0f, 0f, 0f, 0f, false, false);
            ResetPendingInput();
            return;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            if (WasPrimaryMouseButtonPressed())
            {
                HideCursor();
            }

            return;
        }

        float moveX = GetHorizontalMovement();
        float moveZ = GetVerticalMovement();
        Vector2 mouseDelta = GetMouseDelta();

        accumulatedLookX += mouseDelta.x;
        accumulatedLookY += mouseDelta.y;

        if (WasJumpPressed())
        {
            jumpPending = true;
        }

        if (WasPrimaryMouseButtonPressed())
        {
            firePending = true;
        }

        float interval = 1f / Mathf.Max(1, inputSendRate);
        sendAccumulator += Time.unscaledDeltaTime;

        while (sendAccumulator >= interval)
        {
            client.SendInput(++inputSequence, moveX, moveZ, accumulatedLookX, accumulatedLookY, jumpPending, firePending);
            sendAccumulator -= interval;
            ResetPendingInput();
        }
    }

    private float GetHorizontalMovement()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float value = 0f;

            if (Keyboard.current.aKey.isPressed)
            {
                value -= 1f;
            }

            if (Keyboard.current.dKey.isPressed)
            {
                value += 1f;
            }

            return value;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        float value = 0f;

        if (Input.GetKey(KeyCode.A))
        {
            value -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            value += 1f;
        }

        return value;
#endif

        return 0f;
    }

    private float GetVerticalMovement()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null)
        {
            float value = 0f;

            if (Keyboard.current.sKey.isPressed)
            {
                value -= 1f;
            }

            if (Keyboard.current.wKey.isPressed)
            {
                value += 1f;
            }

            return value;
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        float value = 0f;

        if (Input.GetKey(KeyCode.S))
        {
            value -= 1f;
        }

        if (Input.GetKey(KeyCode.W))
        {
            value += 1f;
        }

        return value;
#endif

        return 0f;
    }

    private Vector2 GetMouseDelta()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.delta.ReadValue();
        }
#elif ENABLE_LEGACY_INPUT_MANAGER
        return new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
#endif

        return Vector2.zero;
    }

    private bool WasJumpPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Space);
#else
        return false;
#endif
    }

    private bool WasEscapePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetKeyDown(KeyCode.Escape);
#else
        return false;
#endif
    }

    private bool WasPrimaryMouseButtonPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        return Input.GetMouseButtonDown(0);
#else
        return false;
#endif
    }

    private void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ShowCursor()
    {
        if (Cursor.lockState == CursorLockMode.None && Cursor.visible)
        {
            return;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void ResetPendingInput()
    {
        accumulatedLookX = 0f;
        accumulatedLookY = 0f;
        jumpPending = false;
        firePending = false;
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            return;
        }

        TcpClient client = TcpClient.Instance;

        if (client != null && client.Connected)
        {
            client.SendInput(++inputSequence, 0f, 0f, 0f, 0f, false, false);
        }

        ResetPendingInput();
    }
}