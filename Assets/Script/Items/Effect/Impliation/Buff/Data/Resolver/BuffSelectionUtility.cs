public static class BuffSelectionUtility
{
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
