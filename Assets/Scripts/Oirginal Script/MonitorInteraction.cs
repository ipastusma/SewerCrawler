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
