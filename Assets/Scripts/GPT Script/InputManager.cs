using UnityEngine;
using UnityEngine.InputSystem;

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

    void Update()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            HandleEKey();
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapeKey();
        }

        if (Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }
    }

    void HandleEKey()
    {
        switch (GameStateManager.Instance.CurrentState)
        {
            case GameState.Normal:

                Debug.Log("Normal -> Inventory");

                break;

            case GameState.Inventory:

                Debug.Log("Inventory -> Normal");

                break;

            case GameState.Inspect:

                Debug.Log("Pick Artifact");

                break;

            case GameState.Monitor:

                Debug.Log("Monitor Interaction");

                break;
        }
    }

    void HandleEscapeKey()
    {
        switch (GameStateManager.Instance.CurrentState)
        {
            case GameState.Inventory:

                Debug.Log("Close Inventory");

                break;

            case GameState.Inspect:

                Debug.Log("Exit Inspect");

                break;

            case GameState.Monitor:

                Debug.Log("Exit Monitor");

                break;
        }
    }

    void HandleLeftClick()
    {
        switch (GameStateManager.Instance.CurrentState)
        {
            case GameState.Normal:

                Debug.Log("Interaction Click");

                break;

            case GameState.Inspect:

                Debug.Log("Rotate Item");

                break;
        }
    }
}