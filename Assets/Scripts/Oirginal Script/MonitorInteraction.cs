using UnityEngine;
using UnityEngine.InputSystem;

public class MonitorInteraction : MonoBehaviour
{
    [Header("카메라 설정")]
    public Transform monitorViewPoint;  
    public float transitionSpeed = 5f; 

    private Transform playerCamera;       
    private PlayerController playerController;   
    private bool isZoomed = false;       

    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;

    void Start()
    {
        if (Camera.main != null)
        {
            playerCamera = Camera.main.transform;
            playerController = playerCamera.GetComponentInParent<PlayerController>(); 
        }
    }

    void Update()
    {
        // 1. 확대되지 않은 상태에서 마우스 왼쪽 버튼을 눌렀을 때 클릭 감지
        if (!isZoomed && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleMouseClick();
        }

        // 2. 확대된 상태에서 ESC 키를 누르면 복귀
        if (isZoomed && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ZoomOut();
        }
    }

    // 마우스 클릭 위치로 레이저를 쏘아 본인(모니터)을 맞췄는지 검사
    void HandleMouseClick()
    {
        if (playerController != null && playerController.IsPlayerMoving()) return;

        // 마우스 화면 좌표를 기반으로 3D 공간으로 나아가는 가상의 광선(Ray) 생성
        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePosition);
        RaycastHit hit;

        // 레이저에 무언가 부딪혔을 때
        if (Physics.Raycast(ray, out hit))
        {
            // 부딪힌 오브젝트가 바로 이 스크립트가 붙어있는 오브젝트(모니터)라면 실행
            if (hit.transform == this.transform)
            {
                ZoomIn();
            }
        }
    }

    void ZoomIn()
    {
        isZoomed = true;
        if (playerController != null) playerController.enabled = false;

        originalCameraPosition = playerCamera.position;
        originalCameraRotation = playerCamera.rotation;

        StartCoroutine(MoveCamera(monitorViewPoint.position, monitorViewPoint.rotation));
        Debug.Log("모니터 화면 확대");
    }

    void ZoomOut()
    {
        StartCoroutine(MoveCamera(originalCameraPosition, originalCameraRotation, () =>
        {
            if (playerController != null) playerController.enabled = true;
            isZoomed = false;
            Debug.Log("원래 시점으로 복귀");
        }));
    }

    System.Collections.IEnumerator MoveCamera(Vector3 targetPos, Quaternion targetRot, System.Action callback = null)
    {
        float elapsedTime = 0f;
        float distance = Vector3.Distance(playerCamera.position, targetPos);
        float duration = distance / transitionSpeed;

        if (duration <= 0.01f)
        {
            playerCamera.position = targetPos;
            playerCamera.rotation = targetRot;
            callback?.Invoke();
            yield break;
        }

        Vector3 startPos = playerCamera.position;
        Quaternion startRot = playerCamera.rotation;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            playerCamera.position = Vector3.Lerp(startPos, targetPos, t);
            playerCamera.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }

        playerCamera.position = targetPos;
        playerCamera.rotation = targetRot;
        callback?.Invoke();
    }
}