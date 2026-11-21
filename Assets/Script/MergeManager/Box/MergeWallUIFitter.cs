using UnityEngine;

[ExecuteAlways, DisallowMultipleComponent, DefaultExecutionOrder(210)]
public sealed class MergeWallUIFitter : MonoBehaviour
{
    public MergeBoxBindings box;
    public Camera worldCamera;
    public RectTransform frame;
    [Tooltip("벽마다 별도 UI를 연결하면 해당 Rect 전체를 사용합니다.")]
    public RectTransform leftWall, rightWall, bottomWall;
    public RectTransform topWall;
    public BoxCollider2D topCollider;
    public RectTransform inletAnchor;
    [Tooltip("배경 스프라이트가 상자 UI 프레임 밖으로 나가지 않도록 맞춥니다.")]
    public bool fitBackgroundToFrame = true;
    [SerializeField] private bool showFitGizmos = true;
    [Range(.005f, .2f)] public float leftThickness = .035f;
    [Range(.005f, .2f)] public float rightThickness = .035f;
    [Range(.005f, .2f)] public float bottomThickness = .035f;
    [Range(.005f, .2f)] public float topThickness = .035f;

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
            if (part != null && part.role == MergeBoxSpriteRole.BackFrame)
                FitBackground(part.renderer);
            var wall = part?.wallCollider;
            if (wall == null) continue;
            switch (part.role)
            {
                case MergeBoxSpriteRole.LeftWall:
                    changed |= Fit(wall, leftWall, new Rect(0, 0, leftThickness, 1));
                    break;
                case MergeBoxSpriteRole.RightWall:
                    changed |= Fit(wall, rightWall, new Rect(1 - rightThickness, 0, rightThickness, 1));
                    break;
                case MergeBoxSpriteRole.BottomWall:
                    changed |= Fit(wall, bottomWall, new Rect(0, 0, 1, bottomThickness));
                    break;
            }
        }
        if (topCollider != null)
            changed |= Fit(topCollider, topWall, new Rect(0, 1 - topThickness, 1, topThickness));
        var zone = box.dropZone;
        if (zone != null)
        {
            FitDeadlineWidth(zone.lineSprite);
            var spawn = zone.spawnPoint;
            if (spawn != null && TryGetUICamera(inletAnchor, out var uiCamera) &&
                Project(inletAnchor, uiCamera, new Plane(Vector3.forward, spawn.position), inletAnchor.rect.center, out var inlet))
            {
                var marker = zone.spawnMarker;
                if (marker != null)
                {
                    if ((marker.transform.position - inlet).sqrMagnitude > .00000001f)
                        marker.transform.position = inlet;
                    marker.enabled = marker.sprite != null;
                }
                if ((spawn.position - inlet).sqrMagnitude > .00000001f)
                    spawn.position = inlet;
            }
        }
        if (changed && Application.isPlaying) Physics2D.SyncTransforms();
    }

    private void FitDeadlineWidth(SpriteRenderer line)
    {
        if (line == null || line.drawMode == SpriteDrawMode.Simple || !TryGetUICamera(frame, out var uiCamera)) return;
        var t = line.transform;
        var plane = new Plane(Vector3.forward, t.position);
        var area = frame.rect;
        if (!Project(frame, uiCamera, plane, new Vector2(area.xMin, area.center.y), out var left) ||
            !Project(frame, uiCamera, plane, new Vector2(area.xMax, area.center.y), out var right)) return;
        float width = Mathf.Abs(t.InverseTransformVector(right - left).x);
        if (width < .0001f) return;
        if (Mathf.Abs(line.size.x - width) > .0001f) line.size = new Vector2(width, line.size.y);
        var position = t.position;
        position.x = (left.x + right.x) * .5f - t.TransformVector(line.localBounds.center).x;
        if (Mathf.Abs(position.x - t.position.x) > .0001f) t.position = position;
    }

    private bool Fit(BoxCollider2D wall, RectTransform anchor, Rect edge)
    {
        RectTransform source = anchor != null ? anchor : frame;
        if (!TryGetUICamera(source, out var uiCamera)) return false;

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

    // The card background belongs behind the physics sprites, which an overlay Image would cover.
    private void FitBackground(SpriteRenderer renderer)
    {
        if (!fitBackgroundToFrame || renderer == null || renderer.sprite == null ||
            !TryGetUICamera(frame, out var uiCamera)) return;
        Rect area = ScreenBounds(frame, uiCamera);
        if (area.width <= 0f || area.height <= 0f) return;

        var t = renderer.transform;
        var plane = new Plane(Vector3.forward, t.position);
        if (!ProjectScreen(plane, area.min, out var lowerLeft) ||
            !ProjectScreen(plane, area.max, out var upperRight))
            return;
        Vector3 worldSpan = upperRight - lowerLeft;
        if (renderer.drawMode == SpriteDrawMode.Simple)
        {
            Vector3 spriteSize = renderer.sprite.bounds.size;
            if (spriteSize.x <= 0f || spriteSize.y <= 0f) return;
            Vector3 span = t.parent != null ? t.parent.InverseTransformVector(worldSpan) : worldSpan;
            span = Quaternion.Inverse(t.localRotation) * span;
            Vector3 scale = t.localScale;
            scale.x = Mathf.Abs(span.x) / spriteSize.x * (scale.x < 0f ? -1f : 1f);
            scale.y = Mathf.Abs(span.y) / spriteSize.y * (scale.y < 0f ? -1f : 1f);
            if ((t.localScale - scale).sqrMagnitude > .00000001f) t.localScale = scale;
        }
        else
        {
            Vector3 span = t.InverseTransformVector(worldSpan);
            Vector2 size = new Vector2(Mathf.Abs(span.x), Mathf.Abs(span.y));
            if (size.x <= 0f || size.y <= 0f) return;
            if ((renderer.size - size).sqrMagnitude > .00000001f) renderer.size = size;
        }

        var position = (lowerLeft + upperRight) * .5f - t.TransformVector(renderer.localBounds.center);
        if ((t.position - position).sqrMagnitude > .00000001f) t.position = position;
    }

    private bool TryGetUICamera(RectTransform source, out Camera camera)
    {
        camera = null;
        if (source == null || !source.gameObject.activeInHierarchy) return false;
        var canvas = source.GetComponentInParent<Canvas>()?.rootCanvas;
        if (canvas == null || canvas.targetDisplay != worldCamera.targetDisplay) return false;
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay) return true;
        camera = canvas.worldCamera;
        return camera != null;
    }

    private bool Project(RectTransform source, Camera uiCamera, Plane plane, Vector2 point, out Vector3 world)
    {
        return ProjectScreen(plane, ScreenPoint(source, uiCamera, point), out world);
    }

    private static Vector2 ScreenPoint(RectTransform source, Camera camera, Vector2 point)
    {
        Vector3 world = source.TransformPoint(point);
        return camera != null ? (Vector2)SceneViewportInput.WorldToScreenPoint(camera, world)
            : RectTransformUtility.WorldToScreenPoint(null, world);
    }

    private static Rect ScreenBounds(RectTransform source, Camera camera)
    {
        Rect rect = source.rect;
        Vector2 a = ScreenPoint(source, camera, rect.min);
        Vector2 b = ScreenPoint(source, camera, rect.max);
        Vector2 c = ScreenPoint(source, camera, new Vector2(rect.xMin, rect.yMax));
        Vector2 d = ScreenPoint(source, camera, new Vector2(rect.xMax, rect.yMin));
        Vector2 min = Vector2.Min(Vector2.Min(a, b), Vector2.Min(c, d));
        Vector2 max = Vector2.Max(Vector2.Max(a, b), Vector2.Max(c, d));
        return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
    }

    private bool ProjectScreen(Plane plane, Vector2 point, out Vector3 world)
    {
        Ray ray = SceneViewportInput.ScreenPointToRay(worldCamera, point);
        bool hit = plane.Raycast(ray, out float distance);
        world = ray.GetPoint(distance);
        return hit;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!showFitGizmos || box == null || worldCamera == null) return;
        Color previousColor = Gizmos.color;
        Matrix4x4 previousMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.identity;
        DrawProjectedRect(frame, box.transform.position, Color.yellow);

        foreach (var part in box.visuals)
        {
            if (part == null || part.role != MergeBoxSpriteRole.BackFrame || part.renderer == null) continue;
            var renderer = part.renderer;
            if (!renderer.enabled || renderer.sprite == null) continue;
            Gizmos.color = Color.green;
            Gizmos.matrix = renderer.transform.localToWorldMatrix;
            Bounds bounds = renderer.localBounds;
            Gizmos.DrawWireCube(bounds.center, bounds.size);
            Gizmos.matrix = Matrix4x4.identity;
        }

        Gizmos.matrix = previousMatrix;
        Gizmos.color = previousColor;
    }

    private void DrawProjectedRect(RectTransform source, Vector3 depth, Color color)
    {
        if (!TryGetUICamera(source, out var camera)) return;
        var plane = new Plane(Vector3.forward, depth);
        Rect area = source.rect;
        if (!Project(source, camera, plane, area.min, out var a) ||
            !Project(source, camera, plane, new Vector2(area.xMax, area.yMin), out var b) ||
            !Project(source, camera, plane, area.max, out var c) ||
            !Project(source, camera, plane, new Vector2(area.xMin, area.yMax), out var d)) return;

        Gizmos.color = color;
        Gizmos.DrawLine(a, b);
        Gizmos.DrawLine(b, c);
        Gizmos.DrawLine(c, d);
        Gizmos.DrawLine(d, a);
    }
#endif
}
