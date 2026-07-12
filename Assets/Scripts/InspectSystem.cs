using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InspectSystem : MonoBehaviour
{
    [Header("조사 설정")]
    public Transform inspectPoint;    // 카메라 앞의 목적지
    public GameObject dimOverlay;     // 배경을 어둡게 하는 UI 패널
    public float transitionSpeed = 5f; // 이동 속도
    public float rotationSpeed = 0.5f; // 회전 감도

    [Header("카메라 설정 (오버레이)")]
    public Camera inspectionCamera;

    [Header("인벤토리 설정 (프로토타입)")]
    public string artifactDisplayName = ""; // 인벤토리에 노출될 유물 이름

     // 전역(Static)으로 관리되는 인벤토리 데이터 리스트
    public static List<string> inventoryList = new List<string>();
    public static bool isInventoryOpen = false;

    // 여러 오브젝트가 동시에 키 입력을 중복 처리하는 것을 방지하기 위한 정적 플래그
    private static bool inventoryToggledThisFrame = false;

    private PlayerController playerController;
    private Vector3 originalPos;
    private Quaternion originalRot;
    private Transform originalParent;
    private int originalLayer;

    private bool isInspecting = false;
    private bool isDragging = false;

    void Start()
    {
        if (Camera.main != null)
        {
            playerController = Camera.main.GetComponentInParent<PlayerController>();
        }

        if (inspectionCamera != null)
        {
            inspectionCamera.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 매 프레임 입력 플래그 초기화
        inventoryToggledThisFrame = false;

        if (isInspecting)
        {
            // 자신이 조사 중인 유일한 오브젝트라면 무조건 드래그 및 ESC 복구 처리
            HandleInspection();
        }
        else
        {
            // 인벤토리 창이 열려있는 동안에는 다른 행동을 모두 중단하고 E키로 닫기만 허용
            if (isInventoryOpen)
            {
                if (Keyboard.current.eKey.wasPressedThisFrame && !inventoryToggledThisFrame)
                {
                    inventoryToggledThisFrame = true;
                    ToggleInventory(false);
                }
                return;
            }

            // 조사 중이 아닐 때만 클릭을 검사
            // playerController가 null이 아니고 활성화되어 있을 때만 클릭 처리
            if (playerController != null && playerController.enabled)
            {
                // [E] 키 입력 시 인벤토리 열기
                if (Keyboard.current.eKey.wasPressedThisFrame && !inventoryToggledThisFrame)
                {
                    inventoryToggledThisFrame = true;
                    ToggleInventory(true);
                }
                // 마우스 클릭 시 유물 조사 모드 진입
                else if (Mouse.current.leftButton.wasPressedThisFrame)
                {
                    HandleClick();
                }
            }
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
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartCoroutine(PocketArtifact());
            return;
        }

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
        originalLayer = gameObject.layer;

        SetLayerRecursively(gameObject, LayerMask.NameToLayer("Inspect"));

        if (inspectionCamera != null)
        {
            inspectionCamera.gameObject.SetActive(true);
        }

        // 물체를 카메라의 자식으로 일시 이동 (추후 흔들림 등의 효과가 추가되어 카메라가 움직여도 같이 움직이게 하여 조사 중인 느낌을 주도록)
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

        SetLayerRecursively(gameObject, originalLayer);

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

        if (inspectionCamera != null)
        {
            inspectionCamera.gameObject.SetActive(false);
        }

        if (playerController != null) playerController.enabled = true;
        isInspecting = false;
    }

    IEnumerator PocketArtifact()
    {
        // 1. 전역 인벤토리에 유물 추가
        if (!inventoryList.Contains(artifactDisplayName))
        {
            inventoryList.Add(artifactDisplayName);
        }

        // 2. 원래 부모로 임시 복구 후 물리적/시각적으로 축소 연출
        transform.SetParent(originalParent);

        float elapsedTime = 0;
        float duration = 0.3f;
        Vector3 startScale = transform.localScale;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
            yield return null;
        }

        // 3. 비주얼과 콜라이더만 비활성화 (스크립트 자체는 활성화 상태를 유지하여 E키 열기 기능 보장)
        SetVisualsActive(false);

        if (dimOverlay != null) dimOverlay.SetActive(false);
        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
        if (playerController != null) playerController.enabled = true;

        isInspecting = false;
        Debug.Log($"[{artifactDisplayName}] 획득 완료! 인벤토리에 추가되었습니다.");
    }

    // 평소 화면에서 E키를 누를 때 인벤토리 UI 상태를 조정하는 함수
    void ToggleInventory(bool open)
    {
        isInventoryOpen = open;
        if (dimOverlay != null) dimOverlay.SetActive(open);
        if (playerController != null) playerController.enabled = !open;
    }

    // 오브젝트 수집 시 하이어라키의 본체를 끄는 대신, 렌더러와 물리 충돌만 안전하게 숨겨주는 함수
    void SetVisualsActive(bool active)
    {
        if (GetComponent<Renderer>() != null) GetComponent<Renderer>().enabled = active;
        if (GetComponent<Collider>() != null) GetComponent<Collider>().enabled = active;

        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = active;
        foreach (var c in GetComponentsInChildren<Collider>()) c.enabled = active;
    }

    void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    // 프로토타입 전용 온스크린 2D 렌더링
    void OnGUI()
    {
        if (!isInventoryOpen) return;

        // 화면 중앙 레이아웃 계산
        int width = 450;
        int height = 300;
        int x = (Screen.width - width) / 2;
        int y = (Screen.height - height) / 2;

        // 검은 반투명 박스 그리기
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(2, 2, new Color(0f, 0f, 0f, 0.85f));
        GUI.Box(new Rect(x, y, width, height), "", boxStyle);

        // 타이틀 레이블 스타일
        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.yellow;
        titleStyle.alignment = TextAnchor.UpperCenter;
        GUI.Label(new Rect(x, y + 25, width, 40), "🎒 INVENTORY (PROTOTYPE)", titleStyle);

        // 아이템 텍스트 스타일
        GUIStyle listStyle = new GUIStyle();
        listStyle.fontSize = 18;
        listStyle.normal.textColor = Color.white;
        listStyle.alignment = TextAnchor.UpperCenter;

        if (inventoryList.Count == 0)
        {
            GUI.Label(new Rect(x, y + 120, width, 30), "(인벤토리가 비어 있습니다)", listStyle);
        }
        else
        {
            for (int i = 0; i < inventoryList.Count; i++)
            {
                GUI.Label(new Rect(x, y + 90 + (i * 28), width, 30), $"•  {inventoryList[i]}", listStyle);
            }
        }

        // 하단 조작 안내 스타일
        GUIStyle helpStyle = new GUIStyle();
        helpStyle.fontSize = 14;
        helpStyle.normal.textColor = Color.gray;
        helpStyle.alignment = TextAnchor.LowerCenter;
        GUI.Label(new Rect(x, y + height - 35, width, 25), "[E] 닫기", helpStyle);
    }

    // 단색 배경 이미지를 생성하기 위한 프로토타입 전용 유틸 함수
    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}