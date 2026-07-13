using UnityEngine;

public class InteractionManager : MonoBehaviour
{
    public static InteractionManager Instance { get; private set; }

    // 현재 조사 중인 아이템
    public Inspectable CurrentInspectable { get; private set; }

    // 현재 상호작용 중인 대상
    public IInteractable CurrentInteractable { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // 새로운 상호작용 시작
    public void BeginInteraction(IInteractable interactable)
    {
        CurrentInteractable = interactable;

        interactable.Interact();
    }

    // 조사 시작
    public void BeginInspect(Inspectable inspectable)
    {
        CurrentInspectable = inspectable;

        GameStateManager.Instance.ChangeState(GameState.Inspect);

        inspectable.EnterInspect();
    }

    // 조사 종료
    public void EndInspect()
    {
        if (CurrentInspectable == null)
            return;

        CurrentInspectable.ExitInspect();

        CurrentInspectable = null;

        GameStateManager.Instance.ChangeState(GameState.Normal);
    }

    // 아이템 획득
    public void PickCurrentItem()
    {
        if (CurrentInspectable == null)
            return;

        CurrentInspectable.PickUp();

        CurrentInspectable = null;

        GameStateManager.Instance.ChangeState(GameState.Normal);
    }
}