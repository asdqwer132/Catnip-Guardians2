using UnityEngine;

[CreateAssetMenu(fileName = "SectorMovementControlArea", menuName = "GameData/Items/Effects/Movement/Shapes/Sector")]
public sealed class SectorMovementControlAreaShape : MovementControlAreaShape
{
    [Range(0f, 360f)] public float angle = 90f;
    public override int Overlap(MovementControlAreaContext area, ContactFilter2D filter, Collider2D[] results)
    {
        int count = Physics2D.OverlapCircle(area.center, area.range, filter, results);
        Vector2 forward = area.forward.sqrMagnitude > 0.000001f ? area.forward.normalized : Vector2.right;
        for (int i = 0; i < count; i++)
            if (results[i] != null && Vector2.Angle(forward,
                (Vector2)results[i].transform.position - area.center) > Mathf.Clamp(angle, 0f, 360f) * 0.5f)
                results[i] = null;
        // 원래 조회 개수를 반환해야 꽉 찬 버퍼가 확장되어 부채꼴 안의 뒤쪽 적도 찾는다.
        return count;
    }
}
