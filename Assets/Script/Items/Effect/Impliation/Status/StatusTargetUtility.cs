using UnityEngine;

public enum StatusQueryTarget
{
    PlayerStatus, Owner, HitTarget, SourceItem, SourceBag, PlantHealth, SourceSummon
}

public static class StatusTargetUtility
{
    public static BuffQueryContext Resolve(StatusQueryTarget target, ItemEffectContext context)
    {
        if (context == null) return null;
        switch (target)
        {
            case StatusQueryTarget.PlayerStatus:
                return StatusManager.Instance != null ? BuffQueryContext.ForTarget(StatusManager.Instance) : null;
            case StatusQueryTarget.Owner:
                IBuffTarget owner = context.owner != null ? context.owner.GetComponentInParent<IBuffTarget>() : null;
                return owner != null ? BuffQueryContext.ForTarget(owner) : null;
            case StatusQueryTarget.HitTarget:
                Enemy enemy = context.hitTarget;
                return enemy != null && enemy.CanReceiveHitEffects && enemy.HitEffectLifeId == context.hitTargetLifeId
                    ? BuffQueryContext.ForTarget(enemy) : null;
            case StatusQueryTarget.SourceItem:
                return context.sourceItemData != null ? BuffQueryContext.ForItem(context.sourceItemData, context.sourceBag) : null;
            case StatusQueryTarget.SourceBag:
                return context.sourceBag != null ? BuffQueryContext.ForBag(context.sourceBag) : null;
            case StatusQueryTarget.PlantHealth:
                Plant plant = PlantManager.instance != null ? PlantManager.instance.plant : null;
                return plant != null && plant.health != null && !plant.IsDead
                    ? BuffQueryContext.ForTarget(plant.health) : null;
            case StatusQueryTarget.SourceSummon:
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

    public static bool MatchesSelection(BuffTargetHandle buff, BuffTargetHandle selection)
    {
        if (buff == null || selection == null) return false;
        if (buff.SameTarget(selection)) return true;
        if (selection.kind == BuffTargetKind.Target)
        {
            IBuffTarget target = selection.GetCachedTarget();
            return target != null && buff.MatchesTarget(target);
        }
        if (selection.kind == BuffTargetKind.Item)
            return buff.MatchesItem(selection.itemData, selection.bag);
        if (selection.kind == BuffTargetKind.Bag)
            return buff.MatchesBag(selection.bag);
        return false;
    }

    public static bool HasModifiers(BuffModifier[] modifiers)
    {
        if (modifiers == null) return false;
        for (int i = 0; i < modifiers.Length; i++) if (modifiers[i] != null) return true;
        return false;
    }
}
