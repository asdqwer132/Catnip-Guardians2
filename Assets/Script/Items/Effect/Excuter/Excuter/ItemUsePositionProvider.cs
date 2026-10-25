using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ItemUsePositionProvider : MonoBehaviour
{
    [Header("Camera")]
    public Camera mainCamera;

    [Header("Use Start Position")]
    public Player useStartPoint;

    [Header("Range Clamp")]
    public bool clampMousePositionByPlayerRange = true;

    [Header("목표 위치 지정")]
    [Tooltip("켜면 클릭은 목표 위치만 저장합니다. 아이템 투척은 세션 투척기가 처리합니다.")]
    public bool selectPositionOnly;
    [SerializeField] private bool hasSelectedPosition;
    [SerializeField] private Vector3 selectedPosition;
    public bool HasSelectedPosition => hasSelectedPosition;
    public Vector3 SelectedPosition => selectedPosition;
    public event Action<Vector3> PositionSelected;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

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
        if (selectPositionOnly && hasSelectedPosition)
            return selectedPosition;

        Vector3 mouseWorldPosition = GetRawMouseWorldPosition();

        if (!clampMousePositionByPlayerRange)
            return mouseWorldPosition;

        if (useStartPoint == null)
            return mouseWorldPosition;

        return ClampPositionByPlayerRange(mouseWorldPosition);
    }

    public Vector3 GetRawMouseWorldPosition()
    {
        if (GameInputManager.instance != null)
        {
            Vector3 position = GameInputManager.instance.MouseWorldPosition;
            position.z = 0f;
            return position;
        }

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Vector3 fallbackPosition = transform.position;
            fallbackPosition.z = 0f;
            return fallbackPosition;
        }

        Vector3 worldPosition = Mouse.current != null
            ? mainCamera.ScreenToWorldPoint(Mouse.current.position.ReadValue()) : transform.position;
        worldPosition.z = 0f;

        return worldPosition;
    }

    public bool TryGetTargetPosition(out Vector3 position)
    {
        position = selectPositionOnly ? selectedPosition : GetMouseWorldPosition();
        return !selectPositionOnly || hasSelectedPosition;
    }

    public bool SelectPointerPosition()
    {
        return Mouse.current != null && SelectScreenPosition(Mouse.current.position.ReadValue());
    }

    public bool SelectScreenPosition(Vector2 screenPosition)
    {
        if (mainCamera == null || !mainCamera.isActiveAndEnabled || Time.timeScale <= 0f ||
            !mainCamera.pixelRect.Contains(screenPosition) ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay)))
            return false;

        var presentation = SceneCameraPresentation.ForCamera(mainCamera);
        if (presentation != null && !presentation.ContainsScreenPoint(screenPosition))
            return false;

        // 현재 좌표로 UI를 검사합니다. 지난 프레임의 포인터 판정에 의존하지 않습니다.
        if (EventSystem.current != null)
        {
            uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, uiHits);
            foreach (RaycastResult hit in uiHits)
                if (hit.module is GraphicRaycaster)
                    return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        var plane = new Plane(Vector3.forward, Vector3.zero);
        return plane.Raycast(ray, out float distance) && SelectPosition(ray.GetPoint(distance));
    }

    // 다른 입력에서도 같은 위치 저장/사거리 제한을 사용할 수 있습니다.
    public bool SelectPosition(Vector3 position)
    {
        if (!IsFinite(position.x) || !IsFinite(position.y))
            return false;

        position.z = 0f;
        selectedPosition = clampMousePositionByPlayerRange ? ClampPositionByPlayerRange(position) : position;
        hasSelectedPosition = true;
        PositionSelected?.Invoke(selectedPosition);
        return true;
    }

    public void ClearSelectedPosition() => hasSelectedPosition = false;

    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

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
