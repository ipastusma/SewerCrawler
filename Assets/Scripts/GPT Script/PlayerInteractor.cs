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
