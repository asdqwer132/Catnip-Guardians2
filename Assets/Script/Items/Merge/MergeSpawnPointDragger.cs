using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// 투입구를 잡아서 움직이는 입력만 처리합니다. 아이템 선택 드래그와 같은 클릭을 공유하지 않습니다.
public sealed class MergeSpawnPointDragger : MonoBehaviour
{
    public MergeBoxSceneController boxes;
    public MergegameManager board;
    public MergeItemSelection selection;
    public Camera inputCamera;
    public bool moveX = true;
    public bool moveY;
    [Min(.01f)] public float grabRadius = .3f;

    private InputAction pointer, dragButton;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private MergegameManager draggingBoard;
    private MergeDropZone displayedZone;
    private Vector3 lockedLocalPosition, pointerOffset;
    public bool IsDragging => draggingBoard != null;
    public MergegameManager CurrentBoard => boxes != null ? boxes.SelectedBoard : board;

    private void OnEnable()
    {
        if (pointer == null)
        {
            pointer = new InputAction("Move merge inlet pointer", InputActionType.Value, "<Mouse>/position");
            dragButton = new InputAction("Drag merge inlet", InputActionType.Button, "<Mouse>/leftButton");
            dragButton.performed += _ => BeginPointer();
            dragButton.canceled += _ => EndDrag();
            pointer.performed += _ => MovePointer();
        }
        pointer.Enable(); dragButton.Enable();
    }

    private void Update()
    {
        RefreshMarker();
        MovePointer();
    }

    private void RefreshMarker()
    {
        var current = CurrentBoard;
        var zone = current != null ? current.dropZone : null;
        if (displayedZone != zone && displayedZone != null && displayedZone.spawnMarker != null)
            displayedZone.spawnMarker.enabled = false;
        displayedZone = zone;
        if (zone != null) zone.RefreshMarker(current.NextDropItem);
    }

    private void BeginPointer()
    {
        if (!IsOverUI() && TryWorld(pointer.ReadValue<Vector2>(), out var world)) TryBeginDrag(world);
    }

    private void MovePointer()
    {
        if (!IsDragging) return;
        if (!CanDrag(draggingBoard) || CurrentBoard != draggingBoard || !dragButton.IsPressed() ||
            !TryWorld(pointer.ReadValue<Vector2>(), out var world, false)) { EndDrag(); return; }
        DragTo(world);
    }

    private bool CanDrag(MergegameManager target) => isActiveAndEnabled && target != null && target.CanOperate &&
        target.dropZone != null && target.spawnPoint != null && (moveX || moveY) &&
        (GameInputManager.instance == null || !GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay));

    private bool IsHandleAtWorld(Vector3 world)
    {
        var current = CurrentBoard;
        if (!CanDrag(current)) return false;
        var zone = current.dropZone;
        Vector2 delta = zone.transform.InverseTransformPoint(world) - zone.transform.InverseTransformPoint(current.spawnPoint.position);
        return delta.sqrMagnitude <= grabRadius * grabRadius;
    }

    public bool IsHandleAtScreen(Vector2 screen) => TryWorld(screen, out var world) && IsHandleAtWorld(world);

    public bool TryBeginDrag(Vector3 world)
    {
        if (IsDragging || !IsFinite(world) || !IsHandleAtWorld(world)) return false;
        draggingBoard = CurrentBoard;
        var zone = draggingBoard.dropZone.transform;
        lockedLocalPosition = zone.InverseTransformPoint(draggingBoard.spawnPoint.position);
        pointerOffset = lockedLocalPosition - zone.InverseTransformPoint(world);
        selection?.CancelSelection();
        return true;
    }

    public void DragTo(Vector3 world)
    {
        if (!IsDragging) return;
        if (!IsFinite(world)) return;
        if (!CanDrag(draggingBoard) || CurrentBoard != draggingBoard) { EndDrag(); return; }
        var zone = draggingBoard.dropZone;
        Vector3 target = zone.transform.TransformPoint(zone.transform.InverseTransformPoint(world) + pointerOffset);
        draggingBoard.spawnPoint.position = zone.ConstrainPosition(target, lockedLocalPosition, moveX, moveY);
    }

    public void EndDrag() => draggingBoard = null;

    private bool TryWorld(Vector2 screen, out Vector3 world, bool requireViewport = true)
    {
        world = default;
        var current = CurrentBoard;
        if (!Application.isFocused || current == null || inputCamera == null || !inputCamera.isActiveAndEnabled) return false;
        if (requireViewport && !inputCamera.pixelRect.Contains(screen)) return false;
        var presentation = SceneCameraPresentation.ForCamera(inputCamera);
        if (requireViewport && presentation != null && !presentation.ContainsScreenPoint(screen)) return false;
        var plane = new Plane(Vector3.forward, current.spawnPoint != null ? current.spawnPoint.position : current.transform.position);
        var ray = inputCamera.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance);
        return true;
    }

    private bool IsOverUI()
    {
        var system = EventSystem.current;
        if (system == null) return false;
        var data = new PointerEventData(system) { position = pointer.ReadValue<Vector2>() };
        uiHits.Clear(); system.RaycastAll(data, uiHits);
        foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }

    private void OnApplicationFocus(bool focused) { if (!focused) EndDrag(); }
    private static bool IsFinite(Vector3 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    private void OnDisable()
    {
        EndDrag(); pointer?.Disable(); dragButton?.Disable();
        if (displayedZone != null && displayedZone.spawnMarker != null) displayedZone.spawnMarker.enabled = false;
    }
    private void OnDestroy() { pointer?.Dispose(); dragButton?.Dispose(); }
}
