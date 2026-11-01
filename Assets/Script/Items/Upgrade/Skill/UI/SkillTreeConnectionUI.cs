using UnityEngine;

// 두 선분은 에디터에서 미리 배치합니다. 실행 중에는 생성하지 않고 끝점/표시만 갱신합니다.
[ExecuteAlways]
public sealed class SkillTreeConnectionUI : MonoBehaviour
{
    [Tooltip("기존 규칙과 같이 출발 노드의 해금 상태를 사용합니다.")]
    public SkillNodeUI from;
    public SkillNodeUI to;
    public SkillTreeLineUI horizontalLine;
    public SkillTreeLineUI verticalLine;
    public SkillLineBendMode bendMode;
    public float nodeHalfSize = 40;

    private void LateUpdate() => RefreshGeometry();

    public void SetUnlocked(bool unlocked)
    {
        if (horizontalLine != null) horizontalLine.SetUnlocked(unlocked);
        if (verticalLine != null) verticalLine.SetUnlocked(unlocked);
    }

    public void RefreshGeometry()
    {
        var space = transform as RectTransform;
        var startRect = from != null ? from.transform as RectTransform : null;
        var endRect = to != null ? to.transform as RectTransform : null;
        if (space == null || startRect == null || endRect == null) return;
        Vector2 start = space.InverseTransformPoint(startRect.TransformPoint(startRect.rect.center));
        Vector2 end = space.InverseTransformPoint(endRect.TransformPoint(endRect.rect.center));
        float sx = Mathf.Sign(end.x - start.x), sy = Mathf.Sign(end.y - start.y);
        float half = Mathf.Max(0, nodeHalfSize);
        float startX = Mathf.Abs(space.InverseTransformVector(startRect.TransformVector(Vector3.right * half)).x);
        float startY = Mathf.Abs(space.InverseTransformVector(startRect.TransformVector(Vector3.up * half)).y);
        float endX = Mathf.Abs(space.InverseTransformVector(endRect.TransformVector(Vector3.right * half)).x);
        float endY = Mathf.Abs(space.InverseTransformVector(endRect.TransformVector(Vector3.up * half)).y);
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
        else if (bendMode == SkillLineBendMode.HorizontalThenVertical)
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
