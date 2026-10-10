using UnityEngine;

public enum SpecialStatusQueryTarget
{
    PlayerStatus, Owner, HitTarget, SourceItem, SourceBag, PlantHealth, SourceSummon
}

public static class SpecialStatusTargetUtility
{
    public static BuffQueryContext Resolve(SpecialStatusQueryTarget target, ItemEffectContext context)
    {
        if (context == null) return null;
        switch (target)
        {
            case SpecialStatusQueryTarget.PlayerStatus:
                return StatusManager.Instance != null ? BuffQueryContext.ForTarget(StatusManager.Instance) : null;
            case SpecialStatusQueryTarget.Owner:
                IBuffTarget owner = context.owner != null ? context.owner.GetComponentInParent<IBuffTarget>() : null;
                return owner != null ? BuffQueryContext.ForTarget(owner) : null;
            case SpecialStatusQueryTarget.HitTarget:
                Enemy enemy = context.hitTarget;
                return enemy != null && enemy.CanReceiveHitEffects && enemy.HitEffectLifeId == context.hitTargetLifeId
                    ? BuffQueryContext.ForTarget(enemy) : null;
            case SpecialStatusQueryTarget.SourceItem:
                return context.sourceItemData != null ? BuffQueryContext.ForItem(context.sourceItemData, context.sourceBag) : null;
            case SpecialStatusQueryTarget.SourceBag:
                return context.sourceBag != null ? BuffQueryContext.ForBag(context.sourceBag) : null;
            case SpecialStatusQueryTarget.PlantHealth:
                Plant plant = PlantManager.instance != null ? PlantManager.instance.plant : null;
                return plant != null && plant.health != null && !plant.IsDead
                    ? BuffQueryContext.ForTarget(plant.health) : null;
            case SpecialStatusQueryTarget.SourceSummon:
                SummonItemThrower summon = context.sourceSummon;
                return summon != null && summon.CanAct && summon.LifeId == context.sourceSummonLifeId
                    ? BuffQueryContext.ForTarget(summon) : null;
        }
        return null;
    }

    public static BuffTargetHandle Handle(BuffQueryContext query)
    {
        if (query == null) return null;
        if (query.buffTarget != null) return BuffTargetHandle.Target(query.buffTarget);
        if (query.itemData != null) return BuffTargetHandle.Item(query.itemData);
        return query.bag != null ? BuffTargetHandle.Bag(query.bag) : null;
    }

}
