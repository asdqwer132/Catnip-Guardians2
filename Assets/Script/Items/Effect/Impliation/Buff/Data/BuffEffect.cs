using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffEffect", menuName = "GameData/Item/Item Effect/BuffEffect")]
public class BuffEffect : ItemEffectData
{
    [Header("Target")]
    public BuffTargetResolver targetResolver;

    [Header("Runtime Info")]
    public BuffInfo buffInfo = new BuffInfo();
    public bool includeSelf;
    public bool showInUI = true;

    [Header("Modifiers")]
    public BuffModifier[] modifiers;

    private readonly List<BuffTargetHandle> cachedTargets = new List<BuffTargetHandle>();

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || context.buffManager == null)
            return;

        context.buffManager.RegisterBuff(this, context);
    }

    public List<BuffTargetHandle> ResolveTargets(BuffRegisterContext context)
    {
        cachedTargets.Clear();

        if (targetResolver == null)
            return cachedTargets;

        targetResolver.ResolveTargets(context, cachedTargets);
        return cachedTargets;
    }

    public bool HasValidModifier()
    {
        if (modifiers == null || modifiers.Length <= 0)
            return false;

        for (int i = 0; i < modifiers.Length; i++)
        {
            if (modifiers[i] != null)
                return true;
        }

        return false;
    }
}
