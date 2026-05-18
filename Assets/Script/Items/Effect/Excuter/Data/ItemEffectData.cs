using UnityEngine;

public abstract class ItemEffectData : ScriptableObject
{
    [Header("Impact Visual")]
    public GameObject impactVfxPrefab;
    public bool scaleImpactVfxByRadius = true;
    [Min(0.01f)] public float impactVfxLifeTime = 1f;

    public abstract void Execute(ItemEffectContext context);
}