using UnityEngine;

[DisallowMultipleComponent]
public sealed class PinballOutlet : MonoBehaviour
{
    internal PinballBoard Board { private get; set; }

    private void OnTriggerEnter2D(Collider2D other) => TryExit(other);
    private void OnTriggerStay2D(Collider2D other) => TryExit(other);

    private void TryExit(Collider2D other)
    {
        var piece = other.attachedRigidbody != null ? other.attachedRigidbody.GetComponent<MergeItemObject>() : null;
        if (Board != null && Board.Owns(piece) && transform.InverseTransformPoint(piece.Position).y <= 0f)
            Board.TryExit(piece);
    }
}
