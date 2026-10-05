using UnityEngine;

public struct MovementControlAreaContext
{
    public Vector2 center;
    public Vector2 forward;
    public float range;
}

// 모양 에셋은 설정/기하 연산만 가진다. 탐색 버퍼와 대상 상태는 호출자가 소유한다.
public abstract class MovementControlAreaShape : ScriptableObject
{
    public abstract int Overlap(
        MovementControlAreaContext area,
        ContactFilter2D filter,
        Collider2D[] results
    );

    // 거리 감쇠용 값: 중심 0, 경계 1. 사각형/부채꼴은 필요 시 override한다.
    public virtual float GetNormalizedDistance(MovementControlAreaContext area, Vector2 position)
    {
        return area.range > 0f
            ? Mathf.Clamp01(Vector2.Distance(area.center, position) / area.range)
            : 1f;
    }

    public virtual Vector2 GetVisualSize(MovementControlAreaContext area)
    {
        return Vector2.one * (area.range * 2f);
    }

    public virtual Quaternion GetVisualRotation(MovementControlAreaContext area)
    {
        return Quaternion.identity;
    }
}
