using UnityEngine;

public abstract class BoardPinEffect : ScriptableObject
{
    public abstract void OnHit(PinballPin pin, MergeItemObject piece);
}
