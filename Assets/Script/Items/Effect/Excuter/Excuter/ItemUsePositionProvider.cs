using UnityEngine;
using UnityEngine.InputSystem;

public class ItemUsePositionProvider : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Use Start Position")]
    public Player useStartPoint;

    [Header("Range Clamp")]
    public bool clampMousePositionByPlayerRange = true;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    public Vector3 GetUseStartPosition(GameObject owner)
    {
        Vector3 position;

        if (useStartPoint != null)
            position = useStartPoint.CurrentPosition;
        else if (owner != null)
            position = owner.transform.position;
        else
            position = transform.position;

        position.z = 0f;
        return position;
    }

    public Vector3 GetMouseWorldPosition()
    {
        Vector3 mouseWorldPosition = GetRawMouseWorldPosition();

        if (!clampMousePositionByPlayerRange)
            return mouseWorldPosition;

        if (useStartPoint == null)
            return mouseWorldPosition;

        return ClampPositionByPlayerRange(mouseWorldPosition);
    }

    public Vector3 GetRawMouseWorldPosition()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (Mouse.current == null || mainCamera == null)
        {
            Vector3 fallbackPosition = transform.position;
            fallbackPosition.z = 0f;
            return fallbackPosition;
        }

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();

        Vector3 worldPosition = mainCamera.ScreenToWorldPoint(mouseScreenPosition);
        worldPosition.z = 0f;

        return worldPosition;
    }

    public Vector3 ClampPositionByPlayerRange(Vector3 targetPosition)
    {
        if (useStartPoint == null)
            return targetPosition;

        Vector3 startPosition = useStartPoint.CurrentPosition;
        startPosition.z = 0f;
        targetPosition.z = 0f;

        Vector3 direction = targetPosition - startPosition;
        float distance = direction.magnitude;

        float minRange = useStartPoint.MinRange;
        float maxRange = useStartPoint.MaxRange;

        if (maxRange <= 0f)
            return startPosition;

        if (direction.sqrMagnitude <= 0.0001f)
        {
            Vector3 fallbackDirection = Vector3.down;
            return startPosition + fallbackDirection * minRange;
        }

        direction.Normalize();

        float clampedDistance = Mathf.Clamp(distance, minRange, maxRange);

        Vector3 clampedPosition = startPosition + direction * clampedDistance;
        clampedPosition.z = 0f;

        return clampedPosition;
    }
}