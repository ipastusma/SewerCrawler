using UnityEngine;

/// <summary>마우스 레이캐스트를 한 곳에서 수행하고 상호작용 대상에게만 전달합니다.</summary>
public sealed class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance { get; private set; }
    [SerializeField] private float interactionDistance = 2f;

    [Header("Grid Bounds Settings")]
    [SerializeField] private float gridSize = 1f;

    [Tooltip("문이나 벽 오브젝트 두께를 고려한 판정 여유 범위")]
    [SerializeField] private float margin = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void TryInteract()
    {
        if (PlayerController.Instance == null || PlayerController.Instance.IsPlayerMoving() || Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(UnityEngine.InputSystem.Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance)) return;
        foreach (MonoBehaviour component in hit.collider.GetComponentsInParent<MonoBehaviour>())
        {
            if (!IsInsideFrontGrid(hit.point))
                return;
            if (component is IInteractable interactable)
            {
                InteractionManager.Instance?.BeginInteraction(interactable);
                return;
            }
        }
    }

    private bool IsInsideFrontGrid(Vector3 targetWorldPos)
    {
        // 타격 지점을 플레이어 기준 로컬 좌표로 변환
        Vector3 localPos = PlayerController.Instance.transform.InverseTransformPoint(targetWorldPos);

        float halfGrid = gridSize * 0.5f;

        // X축 (좌우 범위): 정면 타일의 좌우 폭 (-halfGrid ~ +halfGrid)
        bool inX = localPos.x >= -(halfGrid + margin) && localPos.x <= (halfGrid + margin);

        // Z축 (앞뒤 깊이): 1칸 앞 타일의 깊이 (halfGrid ~ 1.5 * gridSize)
        bool inZ = localPos.z >= (halfGrid - margin) && localPos.z <= (gridSize + halfGrid + margin);

        return inX && inZ;
    }
}
