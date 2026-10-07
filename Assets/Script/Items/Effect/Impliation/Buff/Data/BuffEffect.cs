using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffEffect", menuName = "GameData/Items/Effects/Buff")]
public class BuffEffect : ItemEffectData
{
    [Header("Target")]
    public BuffTargetResolver targetResolver;

    [Header("Runtime Info")]
    public BuffInfo buffInfo = new BuffInfo();
    public bool includeSelf;

    [Header("UI")]
    [Tooltip("버프 UI에 표시할 아이콘입니다. 비워두면 버프를 부여한 아이템의 아이콘을 사용합니다.")]
    public Sprite buffIcon;
    public bool showInUI = true;

    [Header("Modifiers")]
    public BuffModifier[] modifiers;

    private readonly List<BuffTargetHandle> cachedTargets = new List<BuffTargetHandle>();

    protected override bool OwnsEndVisual => false;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, buffInfo);
    }

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
