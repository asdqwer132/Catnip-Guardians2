using UnityEngine;

// 입력 장치·카메라와 분리된 월드 좌표 선택 상태입니다. X 취소 후 mouse-up도 무시합니다.
public sealed class MergeItemSelectionGesture
{
    public bool IsActive { get; private set; }
    public bool IsDrag { get; private set; }
    public Vector2 Start { get; private set; }
    public Vector2 End { get; private set; }
    private float nextSelectionAt;
    public Rect Bounds => Rect.MinMaxRect(Mathf.Min(Start.x, End.x), Mathf.Min(Start.y, End.y),
        Mathf.Max(Start.x, End.x), Mathf.Max(Start.y, End.y));
    public float CooldownRemaining(float now) => Mathf.Max(0f, nextSelectionAt - now);

    public bool TryBegin(Vector2 position, float now)
    {
        if (IsActive || CooldownRemaining(now) > 0f || !IsFinite(position)) return false;
        Start = End = position;
        IsDrag = false;
        IsActive = true;
        return true;
    }

    public void Move(Vector2 position, float maximumRange, float dragThreshold)
    {
        if (!IsActive || !IsFinite(position)) return;
        float range = EffectStatUtility.Safe(maximumRange, .01f, 100000f, 3f);
        float threshold = Mathf.Min(range, EffectStatUtility.Safe(dragThreshold, .001f, 100000f, .1f));
        End = Start + Vector2.ClampMagnitude(position - Start, range);
        // 한 번 드래그했다가 시작점으로 돌아와도 클릭으로 바뀌지 않습니다.
        if ((End - Start).sqrMagnitude >= threshold * threshold) IsDrag = true;
    }

    public void Complete(float now, float cooldown)
    {
        if (!IsActive) return;
        // 클릭과 드래그가 같은 다음 선택 시각을 공유하므로 번갈아 써도 우회할 수 없습니다.
        nextSelectionAt = now + EffectStatUtility.Safe(cooldown, 0f, 600f, 0f);
        Cancel();
    }

    public void Cancel() { IsActive = false; IsDrag = false; }

    public static bool IntersectsCircle(Rect rectangle, Vector2 center, float radius)
    {
        Vector2 nearest = new Vector2(Mathf.Clamp(center.x, rectangle.xMin, rectangle.xMax),
            Mathf.Clamp(center.y, rectangle.yMin, rectangle.yMax));
        return (center - nearest).sqrMagnitude <= radius * radius;
    }

    private static bool IsFinite(Vector2 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y);
}
