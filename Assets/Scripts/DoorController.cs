using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class DoorController : MonoBehaviour
{
    [Header("이동 설정")]
    public float openDistance = 1f;    // 이동할 거리
    public float moveDuration = 0.5f;  // 문이 열리는 시간
    
    private bool isOpen = false;       // 문이 열려있는지 상태
    private bool isMoving = false;     // 현재 움직이는 중인지 여부
    
    private Vector3 closedPosition;    // 닫힌 위치 (시작 위치)
    private Vector3 openPosition;      // 열린 위치 (목표 위치)

    private PlayerController playerController;

    void Start()
    {
        // 처음 시작 위치를 닫힌 위치로 저장
        closedPosition = transform.localPosition;
        
        // 로컬 좌표 기준으로 왼쪽(-transform.right)으로 1만큼 이동한 위치를 열린 위치로 계산
        // 유니티에서 -right는 왼쪽입니다.
        openPosition = closedPosition - (Vector3.right * openDistance);

        // 플레이어 컨트롤러 참조 (이동 중 클릭 방지용)
        if (Camera.main != null)
        {
            playerController = Camera.main.GetComponentInParent<PlayerController>();
        }
    }

    void Update()
    {
        // 1. 클릭 입력 감지 (신형 Input System 방식)
        if (!isMoving && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleClick();
        }
    }

    void HandleClick()
    {
        // 플레이어가 이동 중이면 문을 열지 않음
        if (playerController != null && playerController.IsPlayerMoving()) return;

        // 마우스 위치에서 레이저 발사
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // 클릭된 대상이 이 문(나 자신)이라면
            if (hit.transform == this.transform)
            {
                // 상태에 따라 열기 또는 닫기 실행
                if (!isOpen)
                {
                    StartCoroutine(MoveDoor(openPosition));
                }
                else
                {
                    StartCoroutine(MoveDoor(closedPosition));
                }
                
                isOpen = !isOpen; // 상태 반전
            }
        }
    }

    // 문을 부드럽게 이동시키는 코루틴
    IEnumerator MoveDoor(Vector3 targetPos)
    {
        isMoving = true;
        float elapsedTime = 0f;
        Vector3 startPos = transform.localPosition;

        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            // 로컬 좌표를 이동시켜 부모 오브젝트가 회전해도 항상 문의 '왼쪽'으로 열리게 함
            transform.localPosition = Vector3.Lerp(startPos, targetPos, elapsedTime / moveDuration);
            yield return null;
        }

        transform.localPosition = targetPos;
        isMoving = false;
    }
}