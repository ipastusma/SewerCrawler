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
