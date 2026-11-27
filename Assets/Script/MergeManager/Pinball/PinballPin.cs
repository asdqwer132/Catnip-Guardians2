using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballPin : MonoBehaviour
{
    private PinballBoard board;
    private BoardPinData data;

    internal void Bind(PinballBoard owner, BoardPinData definition)
    {
        board = owner;
        data = definition;
    }

    public void QueueMerge(MergeItemObject piece, float radius)
        => board.QueueMerge(piece, transform.position, radius);

    private void OnCollisionEnter2D(Collision2D hit)
    {
        var piece = hit.rigidbody != null ? hit.rigidbody.GetComponent<MergeItemObject>() : null;
        if (data == null || !board.Owns(piece) || piece.IsHeld) return;
        int life = piece.LifeId;
        foreach (var effect in data.hitEffects)
        {
            if (!piece.IsLive || piece.LifeId != life) break;
            if (effect != null) effect.OnHit(this, piece);
        }
    }
}
