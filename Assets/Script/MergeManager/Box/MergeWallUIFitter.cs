using UnityEngine;
using UnityEngine.UI;

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
    public Image backGraphic;
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
        var spawn = box.dropZone != null ? box.dropZone.spawnPoint : null;
        if (spawn != null && inletAnchor != null && TryGetUICamera(inletAnchor, out var uiCamera) &&
            Project(inletAnchor, uiCamera, new Plane(Vector3.forward, spawn.position), inletAnchor.rect.center, out var inlet) &&
            (spawn.position - inlet).sqrMagnitude > .00000001f)
        {
            spawn.position = inlet;
            changed = true;
        }
        if (changed && Application.isPlaying) Physics2D.SyncTransforms();
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
        if (renderer == null || backGraphic == null || backGraphic.sprite == null ||
            !TryGetUICamera(backGraphic.rectTransform, out var uiCamera)) return;
        var source = backGraphic.rectTransform;
        Rect area = source.rect;
        if (area.width <= 0f || area.height <= 0f) return;
        var t = renderer.transform;
        var plane = new Plane(Vector3.forward, t.position);
        if (!Project(source, uiCamera, plane, area.min, out var lowerLeft) ||
            !Project(source, uiCamera, plane, area.max, out var upperRight))
            return;
        var spriteSize = backGraphic.sprite.bounds.size;
        Vector3 span = t.parent != null ? t.parent.InverseTransformVector(upperRight - lowerLeft)
            : upperRight - lowerLeft;
        span = Quaternion.Inverse(t.localRotation) * span;
        if (Mathf.Abs(span.x) < .0001f || Mathf.Abs(span.y) < .0001f ||
            spriteSize.x <= 0f || spriteSize.y <= 0f) return;
        if (renderer.drawMode != SpriteDrawMode.Simple) renderer.drawMode = SpriteDrawMode.Simple;
        if (renderer.sprite != backGraphic.sprite) renderer.sprite = backGraphic.sprite;
        var scale = new Vector3(Mathf.Abs(span.x) / spriteSize.x, Mathf.Abs(span.y) / spriteSize.y, t.localScale.z);
        if ((t.localScale - scale).sqrMagnitude > .00000001f) t.localScale = scale;
        var position = (lowerLeft + upperRight) * .5f - t.TransformVector(renderer.localBounds.center);
        if ((t.position - position).sqrMagnitude > .00000001f) t.position = position;
        if (!renderer.enabled) renderer.enabled = true;
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
