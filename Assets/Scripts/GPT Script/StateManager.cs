using UnityEngine;
using System;

public enum GameState
{
    Normal,
    Inspect,
    Inventory,
    Monitor,
    Dialogue,
    Pause
}

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }

    public GameState CurrentState { get; private set; }

    // 상태가 변경될 때 다른 시스템에게 알려주는 이벤트
    public event Action<GameState> OnStateChanged;

    void Awake()
    {
        // 싱글톤 생성
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentState = GameState.Normal;
    }

    /// 현재 상태인지 확인
    public bool IsState(GameState state)
    {
        return CurrentState == state;
    }

    /// 상태 변경
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
            return;

        GameState previous = CurrentState;
        CurrentState = newState;

        Debug.Log($"GameState : {previous} -> {CurrentState}");

        OnStateChanged?.Invoke(CurrentState);
    }
}