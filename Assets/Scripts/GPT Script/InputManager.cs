using UnityEngine;
using UnityEngine.InputSystem;

public readonly struct GameInput
{
    public readonly bool TogglePressed, CancelPressed, PrimaryPressed, PrimaryHeld;
    public readonly bool MoveForward, MoveBackward, TurnLeft, TurnRight, DebugTeleport;
    public readonly Vector2 PointerDelta;
    public GameInput(Keyboard keyboard, Mouse mouse)
    {
        TogglePressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
        CancelPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        MoveForward = keyboard != null && keyboard.wKey.wasPressedThisFrame;
        MoveBackward = keyboard != null && keyboard.sKey.wasPressedThisFrame;
        TurnLeft = keyboard != null && keyboard.aKey.wasPressedThisFrame;
        TurnRight = keyboard != null && keyboard.dKey.wasPressedThisFrame;
        DebugTeleport = keyboard != null && keyboard.tKey.wasPressedThisFrame;
        PrimaryPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        PrimaryHeld = mouse != null && mouse.leftButton.isPressed;
        PointerDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }
}

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update() => GameStateManager.Instance?.Tick(new GameInput(Keyboard.current, Mouse.current));
}
