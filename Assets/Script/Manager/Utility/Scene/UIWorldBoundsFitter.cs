using System;
using UnityEngine;

// 기존 UI 프레임에 기존 2D 월드 오브젝트를 맞춥니다. UI/카메라/프리팹을 생성하지 않습니다.
[DisallowMultipleComponent]
public sealed class UIWorldBoundsFitter : MonoBehaviour
{
    public RectTransform frame;
    public Camera worldCamera;
    public Transform worldRoot;
    [Tooltip("프레임 내부에서 월드 박스가 차지하는 정규화 영역입니다. X/Y 크기는 프레임 대비 비율로 유지됩니다.")]
    public Rect frameOccupancy = new Rect(.05f, .05f, .9f, .9f);
    public bool captureBoundsFromChildren = true;
    [Tooltip("World Root의 로컬 좌표 기준 범위입니다. 자동 측정을 끄면 이 값을 사용합니다.")]
    public Bounds localBounds = new Bounds(Vector3.zero, Vector3.one);
    [Tooltip("시각 오브젝트/콜라이더 외에 투입구처럼 프레임 안에 포함할 기준점입니다.")]
    public Transform[] includePoints = Array.Empty<Transform>();
    public event Action<Matrix4x4> LayoutChanged;
    private bool captured;
    private readonly Vector3[] corners = new Vector3[4];

    [ContextMenu("기존 박스의 로컬 범위 다시 측정")]
    public void CaptureBounds()
    {
        if (worldRoot == null) return;
        bool hasBounds = false;
        Bounds measured = default;
        foreach (var renderer in worldRoot.GetComponentsInChildren<Renderer>(true))
            if (renderer.enabled && renderer.gameObject.activeInHierarchy) Include(renderer.bounds, ref measured, ref hasBounds);
        foreach (var collider in worldRoot.GetComponentsInChildren<Collider2D>(true))
            if (collider.enabled && collider.gameObject.activeInHierarchy) Include(collider.bounds, ref measured, ref hasBounds);
        if (!hasBounds) return;
        if (includePoints != null) foreach (var point in includePoints)
            if (point != null) measured.Encapsulate(worldRoot.InverseTransformPoint(point.position));
        localBounds = measured;
        captured = true;
    }
    private void Include(Bounds bounds, ref Bounds measured, ref bool initialized)
    {
        // Renderer.bounds는 월드 AABB입니다. 원래 루트 좌표에 고정해 매 프레임 재측정/누적 확대하지 않습니다.
        for (int x = 0; x < 2; x++) for (int y = 0; y < 2; y++)
        {
            Vector3 p = worldRoot.InverseTransformPoint(new Vector3(x == 0 ? bounds.min.x : bounds.max.x,
                y == 0 ? bounds.min.y : bounds.max.y, worldRoot.position.z));
            if (!initialized) { measured = new Bounds(p, Vector3.zero); initialized = true; }
            else measured.Encapsulate(p);
        }
    }
    public bool FitNow()
    {
        if (!isActiveAndEnabled || frame == null || worldRoot == null || worldCamera == null ||
            !frame.gameObject.activeInHierarchy || !worldCamera.isActiveAndEnabled) return false;
        // XY 평면용 컴포넌트입니다. 기울어진 3D 투영을 박스 스케일로 보정하지 않습니다.
        if (!worldCamera.orthographic || Mathf.Abs(Vector3.Dot(worldCamera.transform.forward, Vector3.forward)) < .9999f ||
            Quaternion.Angle(worldRoot.rotation, Quaternion.identity) > .01f) return false;
        if (captureBoundsFromChildren && !captured) CaptureBounds();
        if (localBounds.size.x <= .0001f || localBounds.size.y <= .0001f) return false;
        var canvas = frame.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.rootCanvas.isActiveAndEnabled) return false;
        canvas = canvas.rootCanvas;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null) return false;
        if (canvas.targetDisplay != worldCamera.targetDisplay ||
            (uiCamera != null ? uiCamera.targetTexture : null) != worldCamera.targetTexture) return false;
        Rect area = frame.rect;
        if (area.width <= 0 || area.height <= 0 || frameOccupancy.width <= 0 || frameOccupancy.height <= 0) return false;
        var plane = new Plane(Vector3.forward, new Vector3(0, 0, worldRoot.position.z));
        for (int i = 0; i < 4; i++)
        {
            Vector2 uv = new Vector2(i % 2 == 0 ? frameOccupancy.xMin : frameOccupancy.xMax,
                i < 2 ? frameOccupancy.yMin : frameOccupancy.yMax);
            Vector3 p = frame.TransformPoint(new Vector3(area.xMin + area.width * uv.x, area.yMin + area.height * uv.y, 0));
            Ray ray = worldCamera.ScreenPointToRay(RectTransformUtility.WorldToScreenPoint(uiCamera, p));
            if (!plane.Raycast(ray, out float distance)) return false;
            corners[i] = ray.GetPoint(distance);
        }
        Bounds desired = new Bounds(corners[0], Vector3.zero);
        foreach (var corner in corners) desired.Encapsulate(corner);
        Vector3 size = worldRoot.TransformVector(localBounds.size);
        if (size.x <= .0001f || size.y <= .0001f || desired.size.x <= 0 || desired.size.y <= 0) return false;
        Vector3 scale = worldRoot.localScale;
        scale.x *= desired.size.x / size.x; scale.y *= desired.size.y / size.y;
        Vector3 oldPosition = worldRoot.position, oldScale = worldRoot.localScale;
        Matrix4x4 previous = worldRoot.localToWorldMatrix;
        if (worldRoot.localScale != scale) worldRoot.localScale = scale;
        Vector3 offset = desired.center - worldRoot.TransformPoint(localBounds.center); offset.z = 0;
        if (offset.sqrMagnitude > .0000000001f) worldRoot.position += offset;
        if (worldRoot.position == oldPosition && worldRoot.localScale == oldScale) return false;
        LayoutChanged?.Invoke(worldRoot.localToWorldMatrix * previous.inverse);
        Physics2D.SyncTransforms();
        return true;
    }
}
