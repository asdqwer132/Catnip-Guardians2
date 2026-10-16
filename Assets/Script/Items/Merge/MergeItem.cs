using UnityEngine;

[CreateAssetMenu(fileName = "MergeItem", menuName = "04.Items/Merge/MergeItem")]
public class MergeItem : ScriptableObject
{
    public ItemData item;
    [Min(0.01f)] public float radius = 0.5f;
    public float Radius => EffectStatUtility.Safe(radius, 0.01f, 1000f, 0.5f);
}
