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
