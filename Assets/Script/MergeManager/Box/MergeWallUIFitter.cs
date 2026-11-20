using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(210)]
public sealed class MergeWallUIFitter : MonoBehaviour
{
    public MergeBoxBindings box;
    public Camera worldCamera;
    public RectTransform frame;
    [Tooltip("벽마다 별도 UI를 연결하면 해당 Rect 전체를 사용합니다.")]
    public RectTransform leftWall, rightWall, bottomWall;
    [Range(.005f, .2f)] public float edgeThickness = .035f;

    private void OnEnable() => Canvas.willRenderCanvases += FitWalls;
    private void OnDisable() => Canvas.willRenderCanvases -= FitWalls;
    private void LateUpdate() => FitWalls();

    [ContextMenu("Fit Walls")]
    public void FitWalls()
    {
        if (box == null || worldCamera == null || !worldCamera.isActiveAndEnabled) return;
        bool changed = false;
        foreach (var part in box.visuals)
        {
            var wall = part?.wallCollider;
            if (wall == null) continue;
            float width = edgeThickness;
            switch (part.role)
            {
                case MergeBoxSpriteRole.LeftWall:
                    changed |= Fit(wall, leftWall, new Rect(0, 0, width, 1));
                    break;
                case MergeBoxSpriteRole.RightWall:
                    changed |= Fit(wall, rightWall, new Rect(1 - width, 0, width, 1));
                    break;
                case MergeBoxSpriteRole.BottomWall:
                    changed |= Fit(wall, bottomWall, new Rect(0, 0, 1, width));
                    break;
            }
        }
        if (changed && Application.isPlaying) Physics2D.SyncTransforms();
    }

    private bool Fit(BoxCollider2D wall, RectTransform anchor, Rect edge)
    {
        RectTransform source = anchor != null ? anchor : frame;
        if (source == null || !source.gameObject.activeInHierarchy) return false;
        var canvas = source.GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null || canvas.targetDisplay != worldCamera.targetDisplay) return false;
        bool overlay = canvas.renderMode == RenderMode.ScreenSpaceOverlay;
        Camera uiCamera = overlay ? null : canvas.worldCamera;
        if (!overlay && uiCamera == null) return false;

        Rect area = source.rect;
        if (anchor == null)
            area = new Rect(area.position + Vector2.Scale(area.size, edge.position), Vector2.Scale(area.size, edge.size));
        if (area.width <= 0f || area.height <= 0f) return false;
        var plane = new Plane(Vector3.forward, wall.transform.position);
        if (!Project(source, uiCamera, plane, area.min, out var lowerLeft) ||
            !Project(source, uiCamera, plane, new Vector2(area.xMax, area.yMin), out var lowerRight) ||
            !Project(source, uiCamera, plane, new Vector2(area.xMin, area.yMax), out var upperLeft))
            return false;

        Vector3 horizontal = lowerRight - lowerLeft;
        Vector3 vertical = upperLeft - lowerLeft;
        if (horizontal.sqrMagnitude < .00000001f || vertical.sqrMagnitude < .00000001f) return false;
        var transform = wall.transform;
        Vector3 center = lowerLeft + (horizontal + vertical) * .5f;
        Quaternion rotation = Quaternion.Euler(0, 0, Mathf.Atan2(horizontal.y, horizontal.x) * Mathf.Rad2Deg);
        bool changed = Quaternion.Angle(transform.rotation, rotation) > .01f;
        if (changed) transform.rotation = rotation;
        var size = new Vector2(transform.InverseTransformVector(horizontal).magnitude,
            transform.InverseTransformVector(vertical).magnitude);

        if (wall.TryGetComponent<SpriteRenderer>(out var sprite) && sprite.drawMode != SpriteDrawMode.Simple &&
            (sprite.size - size).sqrMagnitude > .00000001f)
            sprite.size = size;
        Vector2 offset = sprite != null && sprite.drawMode != SpriteDrawMode.Simple
            ? (Vector2)sprite.localBounds.center : Vector2.zero;
        Vector3 position = center - transform.TransformVector(offset);
        if ((transform.position - position).sqrMagnitude > .00000001f)
        {
            transform.position = position;
            changed = true;
        }
        if (wall.autoTiling || (wall.size - size).sqrMagnitude > .00000001f ||
            (wall.offset - offset).sqrMagnitude > .00000001f)
        {
            wall.autoTiling = false;
            wall.size = size;
            wall.offset = offset;
            changed = true;
        }
        return changed;
    }

    private bool Project(RectTransform source, Camera uiCamera, Plane plane, Vector2 point, out Vector3 world)
    {
        Vector3 uiPoint = source.TransformPoint(point);
        Vector2 screenPoint = uiCamera != null
            ? (Vector2)SceneViewportInput.WorldToScreenPoint(uiCamera, uiPoint)
            : RectTransformUtility.WorldToScreenPoint(null, uiPoint);
        Ray ray = SceneViewportInput.ScreenPointToRay(worldCamera, screenPoint);
        bool hit = plane.Raycast(ray, out float distance);
        world = ray.GetPoint(distance);
        return hit;
    }
}
