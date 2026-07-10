using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InspectSystem : MonoBehaviour
{
    [Header("조사 설정")]
    public Transform inspectPoint;    // 카메라 앞의 목적지
    public GameObject dimOverlay;     // 배경을 어둡게 하는 UI 패널
    public float transitionSpeed = 5f; // 이동 속도
    public float rotationSpeed = 0.5f; // 회전 감도

    private PlayerController playerController;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform originalParent;

    private bool isInspecting = false;
    private bool isDragging = false;

    void Start()
    {
        if (Camera.main != null)
        {
            playerController = Camera.main.GetComponentInParent<PlayerController>();
        }
    }

    void Update()
    {
        if (!isInspecting)
        {
            // 1. 일반 상태에서 클릭 감지
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick();
            }
        }
        else
        {
            // 2. 조사 중 상태 제어
            HandleInspection();
        }
    }

    void HandleClick()
    {
        if (playerController != null && playerController.IsPlayerMoving()) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 2.0f))
        {
            if (hit.transform == this.transform)
            {
                StartCoroutine(EnterInspectMode());
            }
        }
    }

    void HandleInspection()
    {
        // ESC 키로 탈출
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            StartCoroutine(ExitInspectMode());
            return;
        }

        // 마우스 드래그 회전 로직
        if (Mouse.current.leftButton.isPressed)
        {
            isDragging = true;
            Vector2 delta = Mouse.current.delta.ReadValue();

            // Y축 이동량으로 X축 회전, X축 이동량으로 Y축 회전
            transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
            transform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
        }
        else
        {
            isDragging = false;
        }
    }

    IEnumerator EnterInspectMode()
    {
        isInspecting = true;
        if (playerController != null) playerController.enabled = false;
        if (dimOverlay != null) dimOverlay.SetActive(true);

        // 원래 상태 저장
        originalPos = transform.position;
        originalRot = transform.rotation;
        originalParent = transform.parent;

        // 물체를 카메라의 자식으로 일시 이동 (카메라가 움직여도 같이 움직이게)
        transform.SetParent(Camera.main.transform);

        float elapsedTime = 0;
        float duration = 0.5f;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            transform.position = Vector3.Lerp(transform.position, inspectPoint.position, t);
            yield return null;
        }

        transform.position = inspectPoint.position;
    }

    IEnumerator ExitInspectMode()
    {
        float elapsedTime = 0;
        float duration = 0.5f;
        if (dimOverlay != null) dimOverlay.SetActive(false);

        // 원래 부모로 복구
        transform.SetParent(originalParent);

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            transform.position = Vector3.Lerp(transform.position, originalPos, t);
            transform.rotation = Quaternion.Lerp(transform.rotation, originalRot, t);
            yield return null;
        }

        transform.position = originalPos;
        transform.rotation = originalRot;

        if (playerController != null) playerController.enabled = true;
        isInspecting = false;
    }
}