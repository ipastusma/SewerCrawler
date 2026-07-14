using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 조사 가능한 유물의 상태 전환만 담당하는 컨텍스트입니다.
/// UI와 인벤토리 데이터는 InventoryOverlay로 분리했습니다.
/// </summary>
public class InspectSystem : MonoBehaviour
{
    [Header("조사 설정")]
    public Transform inspectPoint;
    public GameObject dimOverlay;
    [Min(0.01f)] public float transitionSpeed = 5f;
    public float rotationSpeed = 0.5f;

    [Header("카메라 설정")]
    public Camera inspectionCamera;

    [Header("인벤토리 설정")]
    public string artifactDisplayName = "";

    // 기존 코드에서 읽기 용도로 사용하던 API를 유지합니다.
    public static IReadOnlyList<string> inventoryList => InventoryOverlay.Items;
    public static bool isInventoryOpen => InventoryOverlay.IsOpen;
    public static bool IsInspectionInProgress { get; private set; }

    private PlayerController playerController;
    private InventoryOverlay inventory;
    private Transform mainCameraTransform;
    private InspectionSnapshot snapshot;
    private InspectState currentState;
    private Coroutine transitionRoutine;

    private readonly IdleInspectState idleState = new IdleInspectState();
    private readonly EnteringInspectState enteringState = new EnteringInspectState();
    private readonly ActiveInspectState activeState = new ActiveInspectState();
    private readonly ExitingInspectState exitingState = new ExitingInspectState();
    private readonly CollectingInspectState collectingState = new CollectingInspectState();
    private readonly CollectedInspectState collectedState = new CollectedInspectState();

    internal PlayerController PlayerController => playerController;
    internal bool IsInventoryOpen => inventory != null && inventory.IsVisible;

    private void Awake()
    {
        inventory = InventoryOverlay.GetOrCreate();
        ChangeState(idleState);
    }

    private void Start()
    {
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
            playerController = mainCameraTransform.GetComponentInParent<PlayerController>();
        }

        if (inspectionCamera != null)
            inspectionCamera.gameObject.SetActive(false);
    }

    private void Update()
    {
        currentState.Tick(this);
    }

    private void OnDisable()
    {
        if (currentState != null && currentState != idleState && currentState != collectedState)
        {
            RestoreAfterInspection();
            IsInspectionInProgress = false;
        }
    }

    internal void ChangeState(InspectState nextState)
    {
        currentState?.Exit(this);
        currentState = nextState;

        IsInspectionInProgress = currentState.BlocksInventoryToggle;
        currentState.Enter(this);
    }

    internal void ToggleInventory()
    {
        inventory.Toggle(playerController);
    }

    internal void TryStartInspection()
    {
        if (IsInventoryOpen || playerController == null || !playerController.enabled || playerController.IsPlayerMoving())
            return;

        if (Mouse.current == null || Camera.main == null)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 2f) && hit.transform == transform)
            ChangeState(enteringState);
    }

    internal void BeginEnterTransition()
    {
        snapshot = InspectionSnapshot.Capture(transform);
        SetPlayerControl(false);
        if (dimOverlay != null) dimOverlay.SetActive(true);

        int inspectLayer = LayerMask.NameToLayer("Inspect");
        if (inspectLayer >= 0)
            SetLayerRecursively(gameObject, inspectLayer);

        if (inspectionCamera != null)
            inspectionCamera.gameObject.SetActive(true);

        if (mainCameraTransform != null)
            transform.SetParent(mainCameraTransform, true);

        transitionRoutine = StartCoroutine(MoveToInspectPoint());
    }

    internal void BeginExitTransition()
    {
        transitionRoutine = StartCoroutine(ReturnToOriginalTransform());
    }

    internal void BeginCollection()
    {
        transitionRoutine = StartCoroutine(CollectArtifact());
    }

    internal void StartExiting() => ChangeState(exitingState);

    internal void StartCollecting() => ChangeState(collectingState);

    internal void RotateFromMouse()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        transform.Rotate(Vector3.up, -delta.x * rotationSpeed, Space.World);
        transform.Rotate(Vector3.right, delta.y * rotationSpeed, Space.World);
    }

    private IEnumerator MoveToInspectPoint()
    {
        if (inspectPoint != null)
            yield return MoveTransform(inspectPoint.position, transform.rotation);

        transitionRoutine = null;
        ChangeState(activeState);
    }

    private IEnumerator ReturnToOriginalTransform()
    {
        RestoreParentAndLayer();
        if (dimOverlay != null) dimOverlay.SetActive(false);

        yield return MoveTransform(snapshot.position, snapshot.rotation);
        transitionRoutine = null;
        RestoreAfterInspection();
        ChangeState(idleState);
    }

    private IEnumerator CollectArtifact()
    {
        inventory.Add(artifactDisplayName);
        RestoreParentAndLayer();

        Vector3 startScale = transform.localScale;
        const float duration = 0.3f;
        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, elapsed / duration);
            yield return null;
        }

        transform.localScale = Vector3.zero;
        SetVisualsActive(false);
        transitionRoutine = null;
        RestoreAfterInspection();
        ChangeState(collectedState);
        Debug.Log($"[{artifactDisplayName}] 획득 완료! 인벤토리에 추가되었습니다.");
    }

    private IEnumerator MoveTransform(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = transform.position;
        Quaternion startRotation = transform.rotation;
        float duration = Mathf.Max(0.01f, Vector3.Distance(startPosition, targetPosition) / transitionSpeed);

        for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            transform.SetPositionAndRotation(
                Vector3.Lerp(startPosition, targetPosition, t),
                Quaternion.Lerp(startRotation, targetRotation, t));
            yield return null;
        }

        transform.SetPositionAndRotation(targetPosition, targetRotation);
    }

    private void RestoreParentAndLayer()
    {
        transform.SetParent(snapshot.parent, true);
        SetLayerRecursively(gameObject, snapshot.layer);
    }

    private void RestoreAfterInspection()
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
            transitionRoutine = null;
        }

        RestoreParentAndLayer();
        if (dimOverlay != null) dimOverlay.SetActive(false);
        if (inspectionCamera != null) inspectionCamera.gameObject.SetActive(false);
        SetPlayerControl(true);
    }

    private void SetPlayerControl(bool enabled)
    {
        if (playerController != null)
            playerController.enabled = enabled;
    }

    private void SetVisualsActive(bool active)
    {
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            renderer.enabled = active;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            collider.enabled = active;
    }

    private static void SetLayerRecursively(GameObject target, int layer)
    {
        target.layer = layer;
        foreach (Transform child in target.transform)
            SetLayerRecursively(child.gameObject, layer);
    }

    private readonly struct InspectionSnapshot
    {
        public readonly Vector3 position;
        public readonly Quaternion rotation;
        public readonly Transform parent;
        public readonly int layer;

        private InspectionSnapshot(Transform target)
        {
            position = target.position;
            rotation = target.rotation;
            parent = target.parent;
            layer = target.gameObject.layer;
        }

        public static InspectionSnapshot Capture(Transform target) => new InspectionSnapshot(target);
    }
}

public abstract class InspectState
{
    public virtual bool BlocksInventoryToggle => false;
    public virtual void Enter(InspectSystem context) { }
    public virtual void Tick(InspectSystem context) { }
    public virtual void Exit(InspectSystem context) { }
}

public sealed class IdleInspectState : InspectState
{
    public override void Tick(InspectSystem context)
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            // 다른 유물이 조사 중이면 PlayerController가 비활성화된다.
            // 그 프레임의 E 입력을 인벤토리 열기로 해석하지 않는다.
            if (context.PlayerController == null || context.PlayerController.enabled || context.IsInventoryOpen)
                context.ToggleInventory();
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            context.TryStartInspection();
    }
}

public sealed class EnteringInspectState : InspectState
{
    public override bool BlocksInventoryToggle => true;
    public override void Enter(InspectSystem context) => context.BeginEnterTransition();
}

public sealed class ActiveInspectState : InspectState
{
    public override bool BlocksInventoryToggle => true;
    public override void Tick(InspectSystem context)
    {
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            context.StartCollecting();
        else if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            context.StartExiting();
        else
            context.RotateFromMouse();
    }
}

public sealed class ExitingInspectState : InspectState
{
    public override bool BlocksInventoryToggle => true;
    public override void Enter(InspectSystem context) => context.BeginExitTransition();
}

public sealed class CollectingInspectState : InspectState
{
    public override bool BlocksInventoryToggle => true;
    public override void Enter(InspectSystem context) => context.BeginCollection();
}

public sealed class CollectedInspectState : InspectState { }

/// <summary>
/// 인벤토리 데이터와 프로토타입 표시 UI를 한 곳에서 관리합니다.
/// 조사 오브젝트마다 OnGUI가 실행되던 중복 렌더링 문제를 방지합니다.
/// </summary>
public sealed class InventoryOverlay : MonoBehaviour
{
    private static InventoryOverlay instance;
    private static readonly List<string> items = new List<string>();
    private static int lastToggleFrame = -1;

    public static IReadOnlyList<string> Items => items;
    public static bool IsOpen => instance != null && instance.isVisible;
    public bool IsVisible => isVisible;

    private bool isVisible;
    private Texture2D backgroundTexture;

    public static InventoryOverlay GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<InventoryOverlay>();
        if (instance == null)
        {
            GameObject host = new GameObject("Inventory Overlay");
            instance = host.AddComponent<InventoryOverlay>();
        }
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        backgroundTexture = CreateSolidTexture(new Color(0f, 0f, 0f, 0.85f));
    }

    private void OnDestroy()
    {
        if (backgroundTexture != null)
            Destroy(backgroundTexture);
        if (instance == this)
            instance = null;
    }

    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null ||
            !UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame ||
            InspectSystem.IsInspectionInProgress)
            return;

        // 모든 유물이 획득된 뒤에도 이 오브젝트는 남아 있으므로,
        // 인벤토리를 여닫는 입력은 더 이상 유물에 의존하지 않는다.
        PlayerController player = FindFirstObjectByType<PlayerController>();
        Toggle(player);
    }

    public void Toggle(PlayerController playerController)
    {
        if (Time.frameCount == lastToggleFrame)
            return;

        lastToggleFrame = Time.frameCount;
        SetVisible(!isVisible, playerController);
    }

    public void Add(string artifactName)
    {
        if (!string.IsNullOrWhiteSpace(artifactName) && !items.Contains(artifactName))
            items.Add(artifactName);
    }

    private void SetVisible(bool visible, PlayerController playerController)
    {
        isVisible = visible;
        if (playerController != null)
            playerController.enabled = !visible;
    }

    private void OnGUI()
    {
        if (!isVisible)
            return;

        const int width = 450;
        const int height = 300;
        int x = (Screen.width - width) / 2;
        int y = (Screen.height - height) / 2;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = backgroundTexture;
        GUI.Box(new Rect(x, y, width, height), string.Empty, boxStyle);

        GUIStyle titleStyle = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        titleStyle.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y + 25, width, 40), "INVENTORY (PROTOTYPE)", titleStyle);

        GUIStyle listStyle = new GUIStyle { fontSize = 18, alignment = TextAnchor.UpperCenter };
        listStyle.normal.textColor = Color.white;
        if (items.Count == 0)
            GUI.Label(new Rect(x, y + 120, width, 30), "(인벤토리가 비어 있습니다)", listStyle);
        else
            for (int i = 0; i < items.Count; i++)
                GUI.Label(new Rect(x, y + 90 + i * 28, width, 30), $"•  {items[i]}", listStyle);

        GUIStyle helpStyle = new GUIStyle { fontSize = 14, alignment = TextAnchor.LowerCenter };
        helpStyle.normal.textColor = Color.gray;
        GUI.Label(new Rect(x, y + height - 35, width, 25), "[E] 닫기", helpStyle);
    }

    private static Texture2D CreateSolidTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
