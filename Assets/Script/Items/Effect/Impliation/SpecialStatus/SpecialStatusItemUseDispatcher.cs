using System;
using System.Collections.Generic;

public static class SpecialStatusItemUseDispatcher
{
    private sealed class Reaction
    {
        public ActiveBuff status;
        public BuffStorage storage;
        public SpecialStatusDefinition key;
        public SpecialStatusItemUseModule[] modules;
    }

    public static int Execute(ItemEffectContext context, IReadOnlyList<ActiveBuff> statuses,
        ItemThrowExecutor throwExecutor = null, Func<float> randomValue = null)
    {
        if (context == null || !context.CanContinue || !context.allowItemUseReactions || statuses == null) return 0;
        var reactions = new List<Reaction>();
        var seen = new HashSet<SpecialStatusDefinition>();
        foreach (ActiveBuff status in statuses)
        {
            SpecialStatusDefinition key = status != null ? status.specialStatusKey : null;
            if (key == null || status.IsExpired || !seen.Add(key) || key.onItemUsedModules == null) continue;
            reactions.Add(new Reaction { status = status, storage = status.StorageOwner, key = key,
                modules = (SpecialStatusItemUseModule[])key.onItemUsedModules.Clone() });
        }
        int executed = 0;
        foreach (Reaction reaction in reactions)
        {
            var seenModules = new HashSet<SpecialStatusItemUseModule>();
            foreach (SpecialStatusItemUseModule module in reaction.modules)
            {
                if (!context.CanContinue) return executed;
                if (reaction.status.IsExpired || reaction.status.StorageOwner != reaction.storage ||
                    reaction.status.specialStatusKey != reaction.key) break;
                if (module != null && seenModules.Add(module) && module.TryReact(context, throwExecutor, randomValue)) executed++;
            }
        }
        return executed;
    }
}
