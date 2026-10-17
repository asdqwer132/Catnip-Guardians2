using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MergeItemSelection : MonoBehaviour
{
    [Header("기존 씬 연결")]
    public MergegameManager board;
    public Camera inputCamera;
    [Tooltip("직접 만든 Drag 스프라이트입니다. 없으면 영역 표시 없이 선택만 동작합니다.")]
    public SpriteRenderer dragVisual;

    [Header("클릭·드래그 허용 영역")]
    [Tooltip("직접 만든 UI 영역을 연결하세요. 영역 밖에서는 선택을 시작하지 않으며, 진행 중 마우스가 벗어나면 취소합니다. 비워두면 기존 카메라 영역만 사용합니다.")]
    public RectTransform dragAllowedArea;

    [Header("월드 좌표 선택 범위")]
    [Tooltip("선택 사각형의 최대 가로 길이입니다. 월드 단위이며 카메라 줌과 무관합니다.")]
    [FormerlySerializedAs("maximumDragRange")]
    [Min(.01f)] public float maximumDragWidth = 3f;
    [Tooltip("선택 사각형의 최대 세로 길이입니다. 가로 길이와 독립적으로 제한합니다.")]
    [Min(.01f)] public float maximumDragHeight = 3f;
    [Tooltip("이 거리 이상 움직이면 드래그로 처리합니다. 단위는 월드 거리입니다.")]
    [Min(.001f)] public float dragThreshold = .1f;

    [Header("선택 후 대기 시간 · 초")]
    [Min(0f)] public float dragCooldown = .5f;
    [Min(0f)] public float clickCooldown = .2f;
    public bool logSelections = true;

    private readonly MergeItemSelectionGesture gesture = new MergeItemSelectionGesture();
    private readonly List<Selection> selected = new List<Selection>();
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private InputAction positionInput, selectInput, cancelInput;
    private bool committing;
    private float visualZ;
    private SpriteRenderer boundVisual;
    private Vector3 authoredVisualScale;
    private SpriteDrawMode previousDrawMode;
    private struct Selection { public MergeItemObject piece; public int lifeId; }

    public bool IsSelecting => gesture.IsActive;
    public bool IsDragging => gesture.IsActive && gesture.IsDrag;
    public Rect SelectionBounds => gesture.Bounds;
    public float CooldownRemaining => gesture.CooldownRemaining(Time.time);
    public int LastRemovedCount { get; private set; }
    public string LastMessage { get; private set; }
    private bool CanSelect => isActiveAndEnabled && !committing && board != null && board.CanOperate &&
        (GameInputManager.instance == null || !GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay));

    private void OnEnable()
    {
        BindVisual();
        HideVisual();
        if (positionInput == null)
        {
            positionInput = new InputAction("Merge selection position", InputActionType.Value, "<Mouse>/position");
            selectInput = new InputAction("Select merge items", InputActionType.Button, "<Mouse>/leftButton");
            cancelInput = new InputAction("Cancel merge selection", InputActionType.Button, "<Keyboard>/x");
            selectInput.performed += _ => BeginPointer();
            selectInput.canceled += _ => ReleasePointer();
            cancelInput.performed += _ => CancelSelection();
            // 한 프레임 안에 영역 밖으로 나갔다 돌아와도 취소를 놓치지 않습니다.
            positionInput.performed += _ => UpdatePointerSelection();
        }
        positionInput.Enable(); selectInput.Enable(); cancelInput.Enable();
    }

    private void Update() => UpdatePointerSelection();

    private void UpdatePointerSelection()
    {
        if (!IsSelecting) return;
        if (!CanSelect || !TryPointerWorld(out var world)) { CancelSelection(); return; }
        UpdateSelection(world);
    }

    private void BeginPointer()
    {
        if (TryPointerWorld(out var world) && !IsOverUI()) TryBeginSelection(world);
    }

    private void ReleasePointer()
    {
        if (!IsSelecting) return; // X 취소 후 버튼을 놓아도 클릭으로 실행하지 않습니다.
        if (!TryPointerWorld(out var world) || IsOverUI()) { CancelSelection(); return; }
        CompleteSelection(world);
    }

    private bool TryPointerWorld(out Vector2 world)
    {
        world = default;
        if (!Application.isFocused || inputCamera == null || !inputCamera.isActiveAndEnabled) return false;
        Vector2 screen = positionInput.ReadValue<Vector2>();
        if (!inputCamera.pixelRect.Contains(screen)) return false;
        // 카메라는 마우스 좌표 변환만 담당합니다. 판정과 범위는 아이템 평면의 월드 값입니다.
        var plane = new Plane(Vector3.forward, new Vector3(0f, 0f, board != null ? board.transform.position.z : 0f));
        Ray ray = inputCamera.ScreenPointToRay(screen);
        if (!plane.Raycast(ray, out float distance)) return false;
        world = ray.GetPoint(distance);
        return true;
    }

    private bool IsOverUI()
    {
        var system = EventSystem.current;
        if (system == null) return false;
        // InputAction 콜백에서 지난 프레임의 IsPointerOverGameObject 결과를 사용하지 않습니다.
        var pointer = new PointerEventData(system) { position = positionInput.ReadValue<Vector2>() };
        uiHits.Clear();
        system.RaycastAll(pointer, uiHits);
        foreach (var hit in uiHits) if (hit.module is GraphicRaycaster) return true;
        return false;
    }

    // 다른 입력/UI에서도 월드 좌표로 호출할 수 있습니다. 자동 생성 UI는 사용하지 않습니다.
    public bool TryBeginSelection(Vector2 worldPosition)
    {
        if (!CanSelect || !IsInsideDragArea(worldPosition) || !gesture.TryBegin(worldPosition, Time.time)) return false;
        LastRemovedCount = 0;
        HideVisual();
        return true;
    }

    public void UpdateSelection(Vector2 worldPosition)
    {
        if (!IsSelecting) return;
        // 표시용 사각형을 최대 크기로 줄이기 전에 실제 포인터 위치를 검사합니다.
        if (!CanSelect || !IsInsideDragArea(worldPosition)) { CancelSelection(); return; }
        gesture.Move(worldPosition, maximumDragWidth, maximumDragHeight, dragThreshold);
        RefreshVisual();
    }

    public int CompleteSelection(Vector2 worldPosition)
    {
        if (!IsSelecting || !CanSelect) { CancelSelection(); return 0; }
        UpdateSelection(worldPosition);
        if (!IsSelecting) return 0; // 영역 밖 mouse-up으로 취소됐으면 클릭 삭제로 이어지지 않습니다.
        bool dragged = gesture.IsDrag;
        CollectSelection(dragged);
        gesture.Complete(Time.time, selected.Count > 0 ? (dragged ? dragCooldown : clickCooldown) : 0f);
        HideVisual();
        committing = true;
        LastRemovedCount = 0;
        try
        {
            foreach (var entry in selected)
            {
                var piece = entry.piece;
                if (piece == null || piece.LifeId != entry.lifeId || piece.Owner != board || !piece.CanSelect) continue;
                string name = board.GetItemDisplayName(piece.item);
                Vector2 position = piece.Position;
                if (!board.TryRemoveItem(piece, entry.lifeId)) continue;
                LastRemovedCount++;
                LastMessage = $"{(dragged ? "드래그" : "클릭")} 선택: {name}, 위치: {position}";
                if (logSelections) Debug.Log("[MergeSelection] " + LastMessage, this);
            }
            return LastRemovedCount;
        }
        finally { selected.Clear(); committing = false; }
    }

    public bool IsInsideDragArea(Vector2 worldPosition)
    {
        if (float.IsNaN(worldPosition.x) || float.IsInfinity(worldPosition.x) ||
            float.IsNaN(worldPosition.y) || float.IsInfinity(worldPosition.y)) return false;
        if (dragAllowedArea == null) return true;
        if (!dragAllowedArea.gameObject.activeInHierarchy || inputCamera == null) return false;
        var canvas = dragAllowedArea.GetComponentInParent<Canvas>();
        Camera areaCamera = inputCamera;
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            if (!canvas.isActiveAndEnabled) return false;
            areaCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && areaCamera == null) return false;
        }
        float z = board != null ? board.transform.position.z : 0f;
        Vector3 screen = inputCamera.WorldToScreenPoint(new Vector3(worldPosition.x, worldPosition.y, z));
        if (screen.z < 0f || !RectTransformUtility.ScreenPointToLocalPointInRectangle(dragAllowedArea, screen, areaCamera, out var local)) return false;
        return MergeItemSelectionGesture.ContainsAreaPoint(dragAllowedArea.rect, local);
    }

    private void CollectSelection(bool dragged)
    {
        selected.Clear();
        MergeItemObject clicked = null;
        float closest = float.PositiveInfinity;
        foreach (var piece in board.ActiveItems)
        {
            if (piece == null || piece.Owner != board || !piece.CanSelect || piece.circleCollider == null || !piece.circleCollider.enabled) continue;
            var bounds = piece.circleCollider.bounds;
            if (dragged)
            {
                // 콜라이더 일부만 영역에 걸쳐도 선택합니다. 루트/Visual 크기를 변경하지 않습니다.
                if (MergeItemSelectionGesture.IntersectsCircle(gesture.Bounds, bounds.center, Mathf.Max(bounds.extents.x, bounds.extents.y)))
                    selected.Add(new Selection { piece = piece, lifeId = piece.LifeId });
            }
            else if (piece.circleCollider.OverlapPoint(gesture.End))
            {
                float distance = ((Vector2)bounds.center - gesture.End).sqrMagnitude;
                if (distance <= closest) { closest = distance; clicked = piece; }
            }
        }
        if (clicked != null) selected.Add(new Selection { piece = clicked, lifeId = clicked.LifeId });
    }

    public void CancelSelection()
    {
        bool wasSelecting = IsSelecting;
        gesture.Cancel(); HideVisual();
        if (!wasSelecting) return;
        LastMessage = "선택 취소";
        if (logSelections) Debug.Log("[MergeSelection] " + LastMessage, this);
    }

    private void RefreshVisual()
    {
        if (dragVisual == null || dragVisual.sprite == null) return;
        BindVisual();
        if (dragVisual.enabled != IsDragging) dragVisual.enabled = IsDragging;
        if (!IsDragging) return;
        Rect area = gesture.Bounds;
        Transform visual = dragVisual.transform;
        Sprite sprite = dragVisual.sprite;
        if (visual.rotation != Quaternion.identity) visual.rotation = Quaternion.identity;
        Vector3 localCenter;
        if (dragVisual.drawMode == SpriteDrawMode.Simple)
        {
            Vector3 parentScale = visual.parent != null ? visual.parent.lossyScale : Vector3.one;
            Vector3 spriteSize = sprite.bounds.size;
            Vector3 scale = new Vector3(area.width / Mathf.Max(.0001f, spriteSize.x * Mathf.Abs(parentScale.x)),
                area.height / Mathf.Max(.0001f, spriteSize.y * Mathf.Abs(parentScale.y)), visual.localScale.z);
            if (visual.localScale != scale) visual.localScale = scale;
            localCenter = sprite.bounds.center;
        }
        else
        {
            // Simple에서 Tiled/Sliced로 바꾸면 이전 드래그가 늘린 스케일부터 복구합니다.
            if (previousDrawMode == SpriteDrawMode.Simple) visual.localScale = authoredVisualScale;
            Vector3 scale = visual.lossyScale;
            Vector2 size = new Vector2(area.width / Mathf.Max(.0001f, Mathf.Abs(scale.x)),
                area.height / Mathf.Max(.0001f, Mathf.Abs(scale.y)));
            // 타일/테두리는 늘리지 않고 Renderer의 그리는 영역만 변경합니다.
            if (dragVisual.size != size) dragVisual.size = size;
            Vector2 pivot = sprite.pivot;
            Vector2 spritePixels = sprite.rect.size;
            localCenter = new Vector3((.5f - pivot.x / spritePixels.x) * size.x,
                (.5f - pivot.y / spritePixels.y) * size.y, 0f);
        }
        previousDrawMode = dragVisual.drawMode;
        if (dragVisual.flipX) localCenter.x = -localCenter.x;
        if (dragVisual.flipY) localCenter.y = -localCenter.y;
        Vector3 position = new Vector3(area.center.x, area.center.y, visualZ) - visual.TransformVector(localCenter);
        // 영역이 같으면 Transform/size 값을 다시 쓰지 않아 최대치에서도 표시가 안정됩니다.
        if (visual.position != position) visual.position = position;
    }

    private void BindVisual()
    {
        if (boundVisual == dragVisual) return;
        boundVisual = dragVisual;
        if (boundVisual == null) return;
        authoredVisualScale = boundVisual.transform.localScale;
        visualZ = boundVisual.transform.position.z;
        previousDrawMode = boundVisual.drawMode;
    }

    private void HideVisual()
    {
        if (dragVisual != null) dragVisual.enabled = false;
    }
    private void OnApplicationFocus(bool focused) { if (!focused) CancelSelection(); }
    private void OnDisable()
    {
        CancelSelection();
        positionInput?.Disable(); selectInput?.Disable(); cancelInput?.Disable();
    }
    private void OnDestroy() { positionInput?.Dispose(); selectInput?.Dispose(); cancelInput?.Dispose(); }
}
