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
