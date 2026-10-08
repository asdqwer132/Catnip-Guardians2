using UnityEngine;

[CreateAssetMenu(fileName = "RectangleMovementControlArea", menuName = "GameData/Items/Effects/Movement/Shapes/Rectangle")]
public sealed class RectangleMovementControlAreaShape : MovementControlAreaShape
{
    [Min(0.01f)] public float widthMultiplier = 0.5f;
    public override int Overlap(MovementControlAreaContext area, ContactFilter2D filter, Collider2D[] results)
        => Physics2D.OverlapBox(area.center, GetVisualSize(area),
            Mathf.Atan2(area.forward.y, area.forward.x) * Mathf.Rad2Deg, filter, results);
    public override Vector2 GetVisualSize(MovementControlAreaContext area)
        => new Vector2(area.range * 2f, area.range * EffectStatUtility.Safe(widthMultiplier, 0.01f, 100f, 0.5f));
    public override Quaternion GetVisualRotation(MovementControlAreaContext area)
        => Quaternion.Euler(0f, 0f, Mathf.Atan2(area.forward.y, area.forward.x) * Mathf.Rad2Deg);
}
