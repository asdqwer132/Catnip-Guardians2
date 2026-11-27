using UnityEngine;

[CreateAssetMenu(fileName = "Merge", menuName = "Items/Pinball/Effects/Merge")]
public sealed class BoardPinMergeEffect : BoardPinEffect
{
    [SerializeField, Min(.01f)] private float ingredientRadius = .6f;

    public override void OnHit(PinballPin pin, MergeItemObject piece)
        => pin.QueueMerge(piece, ingredientRadius);
}
