using System.Collections;
using UnityEngine;

/// <summary>격자 이동만 담당합니다. 입력은 InputManager가 상태에 따라 전달합니다.</summary>
public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance { get; private set; }
    [Header("Movement")]
    public float gridSize = 2f;
    public float moveDuration = 0.3f;
    public float rotateDuration = 0.2f;
    [Header("Collision")]
    public LayerMask wallLayer;
    private bool isMoving;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool IsPlayerMoving() => isMoving;
    public void HandleMovement(GameInput input)
    {
        if (isMoving) return;
        if (input.DebugTeleport) { TeleportForDebug(); return; }
        if (input.MoveForward && !HasWall(transform.forward)) StartCoroutine(Move(transform.forward * gridSize));
        else if (input.MoveBackward && !HasWall(-transform.forward)) StartCoroutine(Move(-transform.forward * gridSize));
        else if (input.TurnLeft) StartCoroutine(Rotate(-90f));
        else if (input.TurnRight) StartCoroutine(Rotate(90f));
    }

    private bool HasWall(Vector3 direction) => Physics.Raycast(transform.position, direction, gridSize, wallLayer);
    private void TeleportForDebug()
    {
        if (transform.position == new Vector3(0, 0.5f, -4)) transform.SetPositionAndRotation(new Vector3(0.5f, 0, 0.5f), Quaternion.Euler(0, 90, 0));
        else transform.SetPositionAndRotation(new Vector3(0, 0.5f, -4), Quaternion.identity);
    }

    private IEnumerator Move(Vector3 offset)
    {
        isMoving = true;
        Vector3 start = transform.position;
        Vector3 target = start + offset;
        for (float elapsed = 0; elapsed < moveDuration; elapsed += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(start, target, elapsed / moveDuration);
            yield return null;
        }
        transform.position = new Vector3(Mathf.Round(target.x * 10f) / 10f, Mathf.Round(target.y * 10f) / 10f, Mathf.Round(target.z * 10f) / 10f);
        isMoving = false;
    }

    private IEnumerator Rotate(float angle)
    {
        isMoving = true;
        Quaternion start = transform.rotation;
        Quaternion target = start * Quaternion.Euler(0, angle, 0);
        for (float elapsed = 0; elapsed < rotateDuration; elapsed += Time.deltaTime)
        {
            transform.rotation = Quaternion.Lerp(start, target, elapsed / rotateDuration);
            yield return null;
        }
        transform.rotation = target;
        isMoving = false;
    }
}
