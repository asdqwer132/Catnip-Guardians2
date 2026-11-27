using UnityEngine;

[CreateAssetMenu(fileName = "Bounce", menuName = "Items/Pinball/Effects/Bounce")]
public sealed class BoardPinBounceEffect : BoardPinEffect
{
    [SerializeField, Min(0f)] private float speedBoost = .35f;

    public override void OnHit(PinballPin pin, MergeItemObject piece)
    {
        Vector2 direction = (piece.Position - (Vector2)pin.transform.position).normalized;
        if (Mathf.Abs(direction.x) < .1f)
            direction.x = piece.Serial % 2 == 0 ? .25f : -.25f;
        piece.Velocity += direction.normalized * speedBoost;
    }
}
