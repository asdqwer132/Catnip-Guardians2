using UnityEngine;

[CreateAssetMenu(fileName = "ScaledEffect", menuName = "GameData/Items/Effects/Scaled Effects")]
public sealed class ScaledEffectData : ItemEffectData
{
    public ItemEffectData[] effects;
    [Min(0f)] public float damage = 1f;
    [Min(0f)] public float healing = 1f;
    [Min(0f)] public float range = 1f;
    [Min(0f)] public float duration = 1f;
    public override void ExecuteEffect(ItemEffectContext context)
    {
        ItemEffectContext child = context.Copy(context.targetPosition, context.direction);
        child.damageMultiplier *= EffectExecutionScale.Safe(damage);
        child.healingMultiplier *= EffectExecutionScale.Safe(healing);
        child.rangeMultiplier *= EffectExecutionScale.Safe(range);
        child.durationMultiplier *= EffectExecutionScale.Safe(duration);
        ItemEffectUtility.Execute(effects, child);
    }
}
