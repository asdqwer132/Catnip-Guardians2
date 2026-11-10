using UnityEngine;

// 선분은 배치기가 에디터에서 미리 만듭니다. 실행 중에는 생성하지 않고 위치와 표시만 갱신합니다.
[ExecuteAlways]
public sealed class SkillTreeConnectionUI : MonoBehaviour
{
    [Tooltip("기존 규칙과 같이 출발 노드의 해금 상태를 사용합니다.")]
    public SkillNodeUI from;
    public SkillNodeUI to;
    public SkillTreeLineUI horizontalLine;
    public SkillTreeLineUI verticalLine;
    public SkillLineShape shape;
    public SkillLineEndpoint endpoint;
    public float curveBend = .5f;
    public SkillTreeLineUI[] curveLines = System.Array.Empty<SkillTreeLineUI>();
    private bool unlocked;

    private void LateUpdate() => RefreshGeometry();

    public void SetUnlocked(bool unlocked)
    {
        this.unlocked = unlocked;
        if (curveLines != null) foreach (var line in curveLines) if (line != null) line.SetUnlocked(unlocked);
        if (horizontalLine != null) horizontalLine.SetUnlocked(unlocked);
        if (verticalLine != null) verticalLine.SetUnlocked(unlocked);
    }

    public void RefreshGeometry()
    {
        if (shape != SkillLineShape.Curve && curveLines != null) foreach (var line in curveLines) Hide(line);
        // Keep the controller active so a later unlock can reveal its segments again.
        if (Application.isPlaying &&
            ((from != null && !from.IsVisible) || (to != null && !to.IsVisible)))
        {
            Hide(horizontalLine);
            Hide(verticalLine);
            if (curveLines != null) foreach (var line in curveLines) Hide(line);
            return;
        }
        var space = transform as RectTransform;
        var startRect = from != null ? from.transform as RectTransform : null;
        var endRect = to != null ? to.transform as RectTransform : null;
        if (space == null || startRect == null || endRect == null) return;
        Vector2 start = space.InverseTransformPoint(startRect.TransformPoint(startRect.rect.center));
        Vector2 end = space.InverseTransformPoint(endRect.TransformPoint(endRect.rect.center));
        float sx = Mathf.Sign(end.x - start.x), sy = Mathf.Sign(end.y - start.y);
        bool horizontalFirst = Mathf.Abs(end.x - start.x) >= Mathf.Abs(end.y - start.y);
        Vector2 startHalf = endpoint == SkillLineEndpoint.NodeCenter ? Vector2.zero : startRect.rect.size * .5f;
        Vector2 endHalf = endpoint == SkillLineEndpoint.NodeCenter ? Vector2.zero : endRect.rect.size * .5f;
        float startX = Mathf.Abs(space.InverseTransformVector(startRect.TransformVector(Vector3.right * startHalf.x)).x);
        float startY = Mathf.Abs(space.InverseTransformVector(startRect.TransformVector(Vector3.up * startHalf.y)).y);
        float endX = Mathf.Abs(space.InverseTransformVector(endRect.TransformVector(Vector3.right * endHalf.x)).x);
        float endY = Mathf.Abs(space.InverseTransformVector(endRect.TransformVector(Vector3.up * endHalf.y)).y);
        if (shape != SkillLineShape.Elbow)
        {
            Hide(horizontalLine);
            Hide(verticalLine);
            var delta = end - start;
            var tangent = shape == SkillLineShape.Straight || Mathf.Abs(delta.x) < .01f || Mathf.Abs(delta.y) < .01f
                ? delta.normalized
                : (horizontalFirst ? Vector2.right * sx : Vector2.up * sy);
            var endTangent = shape == SkillLineShape.Straight || Mathf.Abs(delta.x) < .01f || Mathf.Abs(delta.y) < .01f
                ? delta.normalized
                : (horizontalFirst ? Vector2.up * sy : Vector2.right * sx);
            start += EdgeOffset(tangent, startX, startY);
            end -= EdgeOffset(endTangent, endX, endY);
            if (Vector2.Dot(end - start, delta) <= 0) return;
            if (shape == SkillLineShape.Straight)
            {
                SetAngledSegment(horizontalLine, space, start, end);
                return;
            }
            int count = curveLines != null ? curveLines.Length : 0;
            float reach = Vector2.Distance(start, end) * Mathf.Clamp01(curveBend);
            Vector2 c1 = start + tangent * reach, c2 = end - endTangent * reach;
            Vector2 previous = start;
            for (int i = 0; i < count; i++)
            {
                float t = (i + 1f) / count, u = 1 - t;
                Vector2 point = u * u * u * start + 3 * u * u * t * c1 + 3 * u * t * t * c2 + t * t * t * end;
                var line = curveLines[i];
                if (line != null) line.SetUnlocked(unlocked);
                SetAngledSegment(line, space, previous, point);
                previous = point;
            }
            return;
        }
        if (Mathf.Abs(start.y - end.y) < .01f)
        {
            SetSegment(horizontalLine, space, start + Vector2.right * startX * sx, end - Vector2.right * endX * sx, true, sx);
            Hide(verticalLine);
        }
        else if (Mathf.Abs(start.x - end.x) < .01f)
        {
            SetSegment(verticalLine, space, start + Vector2.up * startY * sy, end - Vector2.up * endY * sy, false, sy);
            Hide(horizontalLine);
        }
        else if (horizontalFirst)
        {
            var corner = new Vector2(end.x, start.y);
            SetSegment(horizontalLine, space, start + Vector2.right * startX * sx, corner, true, sx);
            SetSegment(verticalLine, space, corner, end - Vector2.up * endY * sy, false, sy);
        }
        else
        {
            var corner = new Vector2(start.x, end.y);
            SetSegment(verticalLine, space, start + Vector2.up * startY * sy, corner, false, sy);
            SetSegment(horizontalLine, space, corner, end - Vector2.right * endX * sx, true, sx);
        }
    }

    private static Vector2 EdgeOffset(Vector2 direction, float halfX, float halfY)
    {
        float denominator = Mathf.Max(halfX > 0 ? Mathf.Abs(direction.x) / halfX : 0,
            halfY > 0 ? Mathf.Abs(direction.y) / halfY : 0);
        return denominator > .0001f ? direction / denominator : Vector2.zero;
    }

    private static void SetAngledSegment(SkillTreeLineUI line, RectTransform space, Vector2 start, Vector2 end)
    {
        if (line == null || !(line.transform is RectTransform rect)) return;
        var delta = end - start;
        line.gameObject.SetActive(delta.sqrMagnitude > .0001f);
        rect.anchorMin = rect.anchorMax = space.pivot;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = (start + end) * .5f;
        rect.sizeDelta = new Vector2(delta.magnitude, rect.sizeDelta.y);
        rect.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        rect.localScale = Vector3.one;
    }

    private static void Hide(SkillTreeLineUI line) { if (line != null) line.gameObject.SetActive(false); }
    private static void SetSegment(SkillTreeLineUI line, RectTransform space, Vector2 start, Vector2 end, bool horizontal, float direction)
    {
        if (line == null || !(line.transform is RectTransform rect)) return;
        float length = (horizontal ? end.x - start.x : end.y - start.y) * direction;
        line.gameObject.SetActive(length > .01f);
        if (length <= .01f) return;
        rect.anchorMin = rect.anchorMax = space.pivot;
        rect.pivot = new Vector2(.5f, .5f);
        rect.anchoredPosition = (start + end) * .5f;
        var size = rect.sizeDelta;
        if (horizontal) size.x = length; else size.y = length;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }
}
