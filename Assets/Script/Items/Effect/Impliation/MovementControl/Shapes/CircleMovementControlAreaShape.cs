using UnityEngine;

[CreateAssetMenu(fileName = "CircleMovementControlArea", menuName = "04.Items/Effects/Direct Effects/Movement/Shapes/Circle")]
public class CircleMovementControlAreaShape : MovementControlAreaShape
{
    public override int Overlap(MovementControlAreaContext area, ContactFilter2D filter, Collider2D[] results)
    {
        return Physics2D.OverlapCircle(area.center, area.range, filter, results);
    }
}
