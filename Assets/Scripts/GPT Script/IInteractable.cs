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
