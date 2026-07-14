using System.Collections;
using UnityEngine;

/// <summary>
/// 월드 유물의 조사 연출만 담당합니다. 입력, 상태, 인벤토리 저장은 각각 중앙 시스템이 처리합니다.
/// </summary>
public class InspectSystem : Inspectable
{
    public override bool CanAcceptInspectionInput => isInspecting && !isTransitioning;
    [Header("Inspection")]
    public Transform inspectPoint;
    public GameObject dimOverlay;
    [Min(0.01f)] public float transitionSpeed = 5f;
    public float rotationSpeed = 0.5f;
    public Camera inspectionCamera;

    [Header("Item")]
    public ItemData itemData;
    public string artifactId = "";
    public string artifactDisplayName = "";

    private Transform mainCamera;
    private Transform originalParent;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private int originalLayer;
    private bool isInspecting;
    private bool isTransitioning;

    private void Start()
    {
        if (Camera.main != null) mainCamera = Camera.main.transform;
        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
    }

    public override void Interact() => InteractionManager.Instance?.BeginInspect(this);

    public override void EnterInspect()
    {
        if (isInspecting || mainCamera == null || inspectPoint == null) return;
        StartCoroutine(EnterRoutine());
    }

    public override void ExitInspect()
    {
        if (!isInspecting) return;
        StartCoroutine(ExitRoutine());
    }

    public override void PickUp()
    {
        if (!isInspecting || isTransitioning) return;
        StartCoroutine(PickupRoutine());
    }

    public override void Rotate(Vector2 pointerDelta)
    {
        if (!isInspecting || isTransitioning) return;
        transform.Rotate(Vector3.up, -pointerDelta.x * rotationSpeed, Space.World);
        transform.Rotate(Vector3.right, pointerDelta.y * rotationSpeed, Space.World);
    }

    private IEnumerator EnterRoutine()
    {
        isTransitioning = true;
        isInspecting = true;
        originalParent = transform.parent;
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        originalLayer = gameObject.layer;
        if (dimOverlay != null) dimOverlay.SetActive(true);
        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(true);
        int inspectLayer = LayerMask.NameToLayer("Inspect");
        if (inspectLayer >= 0) SetLayerRecursively(gameObject, inspectLayer);
        transform.SetParent(mainCamera, true);
        yield return MoveTransform(inspectPoint.position, transform.rotation);
        isTransitioning = false;
    }

    private IEnumerator ExitRoutine()
    {
        isTransitioning = true;
        RestoreParentAndLayer();
        if (dimOverlay != null) dimOverlay.SetActive(false);
        yield return MoveTransform(originalPosition, originalRotation);
        FinishInspection();
    }

    private IEnumerator PickupRoutine()
    {
        isTransitioning = true;
        InventoryItem item = itemData != null
            ? itemData.CreateRuntimeItem()
            : new InventoryItem(string.IsNullOrWhiteSpace(artifactId) ? artifactDisplayName : artifactId, artifactDisplayName);
        GameEvents.PublishItemPicked(item);
        RestoreParentAndLayer();
        Vector3 startScale = transform.localScale;
        const float duration = 0.3f;
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }
        SetVisualsActive(false);
        FinishInspection();
    }

    private IEnumerator MoveTransform(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float duration = Mathf.Max(0.01f, Vector3.Distance(startPosition, targetPosition) / transitionSpeed);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            transform.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Lerp(startRotation, targetRotation, t));
            yield return null;
        }
        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private void FinishInspection()
    {
        isInspecting = false;
        isTransitioning = false;
        if (dimOverlay != null) dimOverlay.SetActive(false);
        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
    }

    private void RestoreParentAndLayer()
    {
        transform.SetParent(originalParent, true);
        SetLayerRecursively(gameObject, originalLayer);
    }

    private void SetVisualsActive(bool active)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true)) renderer.enabled = active;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true)) collider.enabled = active;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform) SetLayerRecursively(child.gameObject, layer);
    }
}
