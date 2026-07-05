using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // 새로운 Input System 사용
public class PlayerController : MonoBehaviour
{
    [Header("이동 설정")]
    public float gridSize = 2f;   // 한 칸의 크기 (이동 단위의 크기)
    public float moveDuration = 0.3f;   // 한 칸 이동하는 데 걸리는 시간 (초)
    public float rotateDuration = 0.2f;   // 90도 회전하는 데 걸리는 시간 (초)

    private bool isMoving = false;    // 현재 이동/회전 중인지 여부

    void Update()
    {
        if (isMoving) return;

        if (Keyboard.current == null) return; // 키보드가 연결되어 있지 않다면 예외 처리

        if (Keyboard.current.wKey.wasPressedThisFrame)
        {
            StartCoroutine(MovePlayer(transform.forward * gridSize));
        }
        else if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            StartCoroutine(MovePlayer(-transform.forward * gridSize));
        }
        else if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            StartCoroutine(RotatePlayer(-90f));
        }
        else if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            StartCoroutine(RotatePlayer(90f));
        }
    }

    // 부드러운 칸 이동을 위한 코루틴
    IEnumerator MovePlayer(Vector3 direction)
    {
        isMoving = true;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = startPosition + direction;
        float elapsedTime = 0f;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            // Lerp를 이용해 시작점부터 목표점까지 부드럽게 이동
            transform.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / moveDuration);
            yield return null;
        }

        // 이동 후 위치를 소수점 첫째 자리까지 반올림하여 오차를 줄임
        targetPosition.x = Mathf.Round(targetPosition.x * 10f) / 10f;
        targetPosition.y = Mathf.Round(targetPosition.y * 10f) / 10f;
        targetPosition.z = Mathf.Round(targetPosition.z * 10f) / 10f;

        // 오차를 없애기 위해 마지막에 목표 위치로 정확히 고정
        transform.position = targetPosition;
        isMoving = false;
    }

    // 부드러운 90도 회전을 위한 코루틴
    IEnumerator RotatePlayer(float angle)
    {
        isMoving = true;

        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = startRotation * Quaternion.Euler(0, angle, 0);
        float elapsedTime = 0f;

        while (elapsedTime < rotateDuration)
        {
            elapsedTime += Time.deltaTime;
            // Lerp를 이용해 현재 각도에서 목표 각도까지 부드럽게 회전
            transform.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / rotateDuration);
            yield return null;
        }

        // 오차 고정
        transform.rotation = targetRotation;
        isMoving = false;
    }
}