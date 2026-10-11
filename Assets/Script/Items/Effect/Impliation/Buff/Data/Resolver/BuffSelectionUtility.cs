public static class BuffSelectionUtility
{
    public static bool MatchesSelection(BuffTargetHandle buff, BuffTargetHandle selection)
    {
        if (buff == null || selection == null) return false;
        if (selection.kind == BuffTargetKind.AllTargets) return true;
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
        if (buff.kind == BuffTargetKind.AllTargets)
            return selection.kind == BuffTargetKind.AllItems || selection.kind == BuffTargetKind.AllBags ||
                (selection.kind == BuffTargetKind.Group && !string.IsNullOrEmpty(BuffTargetHandle.NormalizeGroupName(selection.targetGroup))) ||
                (selection.kind == BuffTargetKind.ItemSeries && selection.itemSeries != ItemSeries.None);
        return false;
    }

    public static bool HasModifiers(BuffModifier[] modifiers)
    {
        if (modifiers == null) return false;
        for (int i = 0; i < modifiers.Length; i++) if (modifiers[i] != null) return true;
        return false;
    }
}
