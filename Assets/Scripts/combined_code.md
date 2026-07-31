=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\GameBootstrap.cs ===

using UnityEngine;

/// <summary>씬마다 수동으로 매니저를 배치하지 않아도 핵심 서비스를 한 번 생성합니다.</summary>
public static class GameBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateServices()
    {
        GameObject root = new GameObject("Game Services");
        Object.DontDestroyOnLoad(root);
        root.AddComponent<GameStateManager>();
        root.AddComponent<InteractionManager>();
        root.AddComponent<InventoryManager>();
        root.AddComponent<InventoryUI>();
        root.AddComponent<InputManager>();
        root.AddComponent<PlayerInteractor>();
    }
}

=== END OF FILE: GameBootstrap.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\GameEvents.cs ===

using System;

/// <summary>여러 시스템이 같은 게임 사건에 반응할 때 사용하는 최소 이벤트 허브입니다.</summary>
public static class GameEvents
{
    public static event Action<InventoryItem> ItemPicked;
    public static void PublishItemPicked(InventoryItem item) => ItemPicked?.Invoke(item);
}

=== END OF FILE: GameEvents.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\IInteractable.cs ===

public interface IInteractable
{
    void Interact();
}

/// <summary>
/// 조사 모드에 진입할 수 있는 게임 오브젝트의 공통 계약입니다.
/// 구체적인 유물은 이 클래스를 상속하고, 조사 시작/종료/획득 연출을 구현합니다.
/// </summary>
public abstract class Inspectable : UnityEngine.MonoBehaviour, IInteractable
{
    public virtual bool CanAcceptInspectionInput => true;
    public virtual void Interact()
    {
        if (InteractionManager.Instance == null)
        {
            UnityEngine.Debug.LogWarning($"InteractionManager가 없어 {name}을(를) 조사할 수 없습니다.", this);
            return;
        }

        InteractionManager.Instance.BeginInspect(this);
    }

    /// <summary>조사 화면으로 전환할 때 호출됩니다.</summary>
    public abstract void EnterInspect();

    /// <summary>조사를 취소하고 원래 상태로 복귀할 때 호출됩니다.</summary>
    public abstract void ExitInspect();

    /// <summary>조사 중인 오브젝트를 획득할 때 호출됩니다.</summary>
    public abstract void PickUp();

    public abstract void Rotate(UnityEngine.Vector2 pointerDelta);
}

=== END OF FILE: IInteractable.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\InputManager.cs ===

using UnityEngine;
using UnityEngine.InputSystem;

public readonly struct GameInput
{
    public readonly bool TogglePressed, CancelPressed, PrimaryPressed, PrimaryHeld;
    public readonly bool MoveForward, MoveBackward, TurnLeft, TurnRight, DebugTeleport;
    public readonly Vector2 PointerDelta;
    public GameInput(Keyboard keyboard, Mouse mouse)
    {
        TogglePressed = keyboard != null && keyboard.eKey.wasPressedThisFrame;
        CancelPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;
        MoveForward = keyboard != null && keyboard.wKey.wasPressedThisFrame;
        MoveBackward = keyboard != null && keyboard.sKey.wasPressedThisFrame;
        TurnLeft = keyboard != null && keyboard.aKey.wasPressedThisFrame;
        TurnRight = keyboard != null && keyboard.dKey.wasPressedThisFrame;
        DebugTeleport = keyboard != null && keyboard.tKey.wasPressedThisFrame;
        PrimaryPressed = mouse != null && mouse.leftButton.wasPressedThisFrame;
        PrimaryHeld = mouse != null && mouse.leftButton.isPressed;
        PointerDelta = mouse != null ? mouse.delta.ReadValue() : Vector2.zero;
    }
}

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

    private void Update() => GameStateManager.Instance?.Tick(new GameInput(Keyboard.current, Mouse.current));
}

=== END OF FILE: InputManager.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\InteractionManager.cs ===

using UnityEngine;

/// <summary>현재 진행 중인 단 하나의 상호작용을 소유합니다.</summary>
public sealed class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }
    public Inspectable CurrentInspectable { get; private set; }
    public MonitorInteraction CurrentMonitor { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void BeginInteraction(IInteractable interactable) => interactable?.Interact();

    public bool BeginInspect(Inspectable inspectable)
    {
        if (inspectable == null || CurrentInspectable != null) return false;
        CurrentInspectable = inspectable;
        GameStateManager.Instance.ChangeState(GameState.Inspect);
        inspectable.EnterInspect();
        return true;
    }

    public void EndInspect()
    {
        if (CurrentInspectable == null || !CurrentInspectable.CanAcceptInspectionInput) return;
        CurrentInspectable.ExitInspect();
        CurrentInspectable = null;
        GameStateManager.Instance.ChangeState(GameState.Normal);
    }

    public void PickCurrentItem()
    {
        if (CurrentInspectable == null || !CurrentInspectable.CanAcceptInspectionInput) return;
        Inspectable item = CurrentInspectable;
        CurrentInspectable = null;
        item.PickUp();
        GameStateManager.Instance.ChangeState(GameState.Normal);
    }

    public void RotateCurrentInspectable(Vector2 pointerDelta) => CurrentInspectable?.Rotate(pointerDelta);

    public bool BeginMonitor(MonitorInteraction monitor)
    {
        if (monitor == null || CurrentMonitor != null) return false;
        CurrentMonitor = monitor;
        GameStateManager.Instance.ChangeState(GameState.Monitor);
        monitor.EnterMonitor();
        return true;
    }

    public void EndMonitor()
    {
        if (CurrentMonitor == null) return;
        MonitorInteraction monitor = CurrentMonitor;
        CurrentMonitor = null;
        monitor.ExitMonitor();
        GameStateManager.Instance.ChangeState(GameState.Normal);
    }
}

=== END OF FILE: InteractionManager.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\InventoryItem.cs ===

using UnityEngine;

[System.Serializable]
public class InventoryItem
{
    public string itemID;
    public string displayName;

    public InventoryItem(string id, string name)
    {
        itemID = id;
        displayName = name;
    }
}

=== END OF FILE: InventoryItem.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\InventoryManager.cs ===

using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>인벤토리 데이터만 보관하며 UI나 입력을 처리하지 않습니다.</summary>
public sealed class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    private readonly List<InventoryItem> items = new List<InventoryItem>();
    public IReadOnlyList<InventoryItem> Items => items;
    public event Action InventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable() => GameEvents.ItemPicked += HandleItemPicked;
    private void OnDisable() => GameEvents.ItemPicked -= HandleItemPicked;

    private void HandleItemPicked(InventoryItem item) => AddItem(item);

    public bool AddItem(InventoryItem item)
    {
        if (item == null || HasItem(item.itemID)) return false;
        items.Add(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool RemoveItem(string itemId)
    {
        InventoryItem item = GetItem(itemId);
        if (item == null) return false;
        items.Remove(item);
        InventoryChanged?.Invoke();
        return true;
    }

    public bool HasItem(string itemId) => GetItem(itemId) != null;
    public InventoryItem GetItem(string itemId) => items.Find(item => item.itemID == itemId);
}

=== END OF FILE: InventoryManager.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\InventoryUI.cs ===

using UnityEngine;

/// <summary>GameState와 InventoryManager를 구독해 표시만 담당하는 프로토타입 UI입니다.</summary>
public sealed class InventoryUI : MonoBehaviour
{
    private Texture2D background;
    private void Awake()
    {
        background = new Texture2D(1, 1);
        background.SetPixel(0, 0, new Color(0, 0, 0, .85f));
        background.Apply();
    }
    private void OnDestroy() { if (background != null) Destroy(background); }
    private void OnGUI()
    {
        if (GameStateManager.Instance == null || !GameStateManager.Instance.IsState(GameState.Inventory)) return;
        const int width = 450, height = 300;
        int x = (Screen.width - width) / 2, y = (Screen.height - height) / 2;
        GUIStyle box = new GUIStyle(GUI.skin.box); box.normal.background = background;
        GUI.Box(new Rect(x, y, width, height), string.Empty, box);
        GUIStyle title = new GUIStyle { fontSize = 24, fontStyle = FontStyle.Bold, alignment = TextAnchor.UpperCenter };
        title.normal.textColor = Color.yellow;
        GUI.Label(new Rect(x, y + 25, width, 40), "INVENTORY (PROTOTYPE)", title);
        GUIStyle list = new GUIStyle { fontSize = 18, alignment = TextAnchor.UpperCenter }; list.normal.textColor = Color.white;
        var items = InventoryManager.Instance == null ? null : InventoryManager.Instance.Items;
        if (items == null || items.Count == 0) GUI.Label(new Rect(x, y + 120, width, 30), "(인벤토리가 비어 있습니다)", list);
        else for (int i = 0; i < items.Count; i++) GUI.Label(new Rect(x, y + 90 + 28 * i, width, 30), $"•  {items[i].displayName}", list);
        GUI.Label(new Rect(x, y + height - 35, width, 25), "[E] 또는 [ESC] 닫기", new GUIStyle(list) { fontSize = 14, alignment = TextAnchor.LowerCenter });
    }
}

=== END OF FILE: InventoryUI.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\ItemData.cs ===

using UnityEngine;

[CreateAssetMenu(menuName = "Sewer Crawler/Item Data", fileName = "NewItem")]
public sealed class ItemData : ScriptableObject
{
    [SerializeField] private string itemId;
    [SerializeField] private string displayName;
    [SerializeField] private Sprite icon;
    [TextArea] [SerializeField] private string description;
    public string ItemId => itemId;
    public string DisplayName => displayName;
    public Sprite Icon => icon;
    public string Description => description;
    public InventoryItem CreateRuntimeItem() => new InventoryItem(itemId, displayName);
}

=== END OF FILE: ItemData.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\PlayerInteractor.cs ===

using UnityEngine;

/// <summary>마우스 레이캐스트를 한 곳에서 수행하고 상호작용 대상에게만 전달합니다.</summary>
public sealed class PlayerInteractor : MonoBehaviour
{
    public static PlayerInteractor Instance { get; private set; }
    [SerializeField] private float interactionDistance = 2f;
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
            if (component is IInteractable interactable)
            {
                InteractionManager.Instance?.BeginInteraction(interactable);
                return;
            }
        }
    }
}

=== END OF FILE: PlayerInteractor.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\GPT Script\StateManager.cs ===

using System;
using UnityEngine;

public enum GameState { Normal, Inspect, Inventory, Monitor, Dialogue, Pause }

public abstract class GameFlowState
{
    protected readonly GameStateManager Manager;
    public abstract GameState Id { get; }
    protected GameFlowState(GameStateManager manager) => Manager = manager;
    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick(GameInput input) { }
}

public sealed class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    public GameState CurrentState => currentState.Id;
    public event Action<GameState, GameState> StateChanged;
    private GameFlowState currentState;
    private GameFlowState normalState, inspectState, inventoryState, monitorState;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        normalState = new NormalGameState(this);
        inspectState = new InspectGameState(this);
        inventoryState = new InventoryGameState(this);
        monitorState = new MonitorGameState(this);
        currentState = normalState;
        currentState.Enter();
    }

    public bool IsState(GameState state) => CurrentState == state;
    public void Tick(GameInput input) => currentState.Tick(input);
    public void ChangeState(GameState next)
    {
        GameFlowState destination = GetState(next);
        if (destination == currentState) return;
        GameState previous = CurrentState;
        currentState.Exit();
        currentState = destination;
        currentState.Enter();
        StateChanged?.Invoke(previous, CurrentState);
    }

    private GameFlowState GetState(GameState state) => state switch
    {
        GameState.Inspect => inspectState,
        GameState.Inventory => inventoryState,
        GameState.Monitor => monitorState,
        _ => normalState
    };
}

internal sealed class NormalGameState : GameFlowState
{
    public override GameState Id => GameState.Normal;
    public NormalGameState(GameStateManager manager) : base(manager) { }
    public override void Tick(GameInput input)
    {
        if (input.TogglePressed) { Manager.ChangeState(GameState.Inventory); return; }
        PlayerController.Instance?.HandleMovement(input);
        if (input.PrimaryPressed) PlayerInteractor.Instance?.TryInteract();
    }
}

internal sealed class InspectGameState : GameFlowState
{
    public override GameState Id => GameState.Inspect;
    public InspectGameState(GameStateManager manager) : base(manager) { }
    public override void Tick(GameInput input)
    {
        if (input.CancelPressed) { InteractionManager.Instance?.EndInspect(); return; }
        if (input.TogglePressed) { InteractionManager.Instance?.PickCurrentItem(); return; }
        if (input.PrimaryHeld) InteractionManager.Instance?.RotateCurrentInspectable(input.PointerDelta);
    }
}

internal sealed class InventoryGameState : GameFlowState
{
    public override GameState Id => GameState.Inventory;
    public InventoryGameState(GameStateManager manager) : base(manager) { }
    public override void Tick(GameInput input)
    {
        if (input.TogglePressed || input.CancelPressed) Manager.ChangeState(GameState.Normal);
    }
}

internal sealed class MonitorGameState : GameFlowState
{
    public override GameState Id => GameState.Monitor;
    public MonitorGameState(GameStateManager manager) : base(manager) { }
    public override void Tick(GameInput input)
    {
        if (input.CancelPressed) InteractionManager.Instance?.EndMonitor();
    }
}

=== END OF FILE: StateManager.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\Oirginal Script\ComputerOS.cs ===

using UnityEngine;
// UI 요소를 제어하기 위해 반드시 필요한 네임스페이스입니다.
using UnityEngine.UI; 

public class ComputerOS : MonoBehaviour
{
    [Header("UI 패널 설정")]
    // 바탕화면 위에 뜰 기사창과 이메일창 오브젝트를 담는 변수.
    // 인스펙터에서 직접 드래그 앤 드롭으로 연결.
    public GameObject newsPanel;  
    public GameObject emailPanel; 

    // 게임이 시작될 때 단 한 번 실행.
    void Start()
    {
        // 컴퓨터를 처음 켰을 때 창이 다 열려 있으면 안 되므로 초기화.
        CloseAllWindows();
    }

    // [OpenNews] 인터넷 아이콘 버튼에 연결할 함수.
    // public이 붙어야 유니티 버튼의 OnClick 이벤트에서 이 함수를 찾을 수 있음.
    public void OpenNews()
    {
        CloseAllWindows();      // 다른 창이 열려 있다면 먼저 다 닫는다.
        newsPanel.SetActive(true); // 뉴스 패널만 활성화한다.
        Debug.Log("뉴스 기사를 불러왔습니다.");
    }

    // [OpenEmail] 이메일 아이콘 버튼에 연결할 함수.
    public void OpenEmail()
    {
        CloseAllWindows();
        emailPanel.SetActive(true); // 이메일 패널만 활성화.
        Debug.Log("이메일 시스템에 접속했습니다.");
    }

    // 모든 창을 끄는 로직.
    // 중복 코드를 줄이기 위해 별도의 함수로 생성.
    public void CloseAllWindows()
    {
        // .SetActive(false)는 오브젝트를 Hide 처리하고 연산을 중지.
        if(newsPanel != null) newsPanel.SetActive(false);
        if(emailPanel != null) emailPanel.SetActive(false);
    }
}

=== END OF FILE: ComputerOS.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\Oirginal Script\DoorController.cs ===

using System.Collections;
using UnityEngine;

/// <summary>문 애니메이션만 담당합니다. 클릭 판정은 PlayerInteractor가 수행합니다.</summary>
public class DoorController : MonoBehaviour, IInteractable
{
    public float openDistance = 1f;
    public float moveDuration = 0.5f;
    private Vector3 closedPosition;
    private Vector3 openPosition;
    private bool isOpen;
    private bool isMoving;

    private void Start()
    {
        closedPosition = transform.localPosition;
        openPosition = closedPosition - Vector3.right * openDistance;
    }

    public void Interact()
    {
        if (isMoving) return;
        isOpen = !isOpen;
        StartCoroutine(MoveTo(isOpen ? openPosition : closedPosition));
    }

    private IEnumerator MoveTo(Vector3 target)
    {
        isMoving = true;
        Vector3 start = transform.localPosition;
        for (float elapsed = 0; elapsed < moveDuration; elapsed += Time.deltaTime)
        {
            transform.localPosition = Vector3.Lerp(start, target, elapsed / moveDuration);
            yield return null;
        }
        transform.localPosition = target;
        isMoving = false;
    }
}

=== END OF FILE: DoorController.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\Oirginal Script\InspectSystem.cs ===

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

=== END OF FILE: InspectSystem.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\Oirginal Script\MonitorInteraction.cs ===

using System.Collections;
using UnityEngine;

/// <summary>모니터 시점 전환만 담당합니다. ESC 처리는 MonitorGameState가 담당합니다.</summary>
public class MonitorInteraction : MonoBehaviour, IInteractable
{
    public Transform monitorViewPoint;
    [Min(0.01f)] public float transitionSpeed = 5f;
    private Transform playerCamera;
    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private void Start() { if (Camera.main != null) playerCamera = Camera.main.transform; }
    public void Interact() => InteractionManager.Instance?.BeginMonitor(this);
    public void EnterMonitor()
    {
        if (playerCamera == null || monitorViewPoint == null) return;
        originalPosition = playerCamera.position;
        originalRotation = playerCamera.rotation;
        StartCoroutine(MoveCamera(monitorViewPoint.position, monitorViewPoint.rotation));
    }
    public void ExitMonitor() { if (playerCamera != null) StartCoroutine(MoveCamera(originalPosition, originalRotation)); }
    private IEnumerator MoveCamera(Vector3 targetPosition, Quaternion targetRotation)
    {
        Vector3 startPosition = playerCamera.position;
        Quaternion startRotation = playerCamera.rotation;
        float duration = Mathf.Max(0.01f, Vector3.Distance(startPosition, targetPosition) / transitionSpeed);
        for (float elapsed = 0; elapsed < duration; elapsed += Time.deltaTime)
        {
            float t = elapsed / duration;
            playerCamera.SetPositionAndRotation(Vector3.Lerp(startPosition, targetPosition, t), Quaternion.Lerp(startRotation, targetRotation, t));
            yield return null;
        }
        playerCamera.SetPositionAndRotation(targetPosition, targetRotation);
    }
}

=== END OF FILE: MonitorInteraction.cs ===

=== FILE: C:\UnityWorkspace\GitSewerCrawler\Assets\Scripts\Oirginal Script\PlayerController.cs ===

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

=== END OF FILE: PlayerController.cs ===

