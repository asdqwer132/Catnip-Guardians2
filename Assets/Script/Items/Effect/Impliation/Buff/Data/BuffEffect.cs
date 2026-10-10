using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffEffect", menuName = "04.Items/Effects/Direct Effects/Buff/Buff")]
public class BuffEffect : ItemEffectData
{
    [Header("Target")]
    public BuffTargetResolver targetResolver;

    [Header("Runtime Info")]
    [InspectorName("Buff Lifetime / Reapplication")]
    public BuffInfo buffInfo = new BuffInfo();
    public bool includeSelf;

    [Header("Buff Classification")]
    [Tooltip("공유할 해로움·정화 가능 여부·정화 태그입니다. 특수 상태는 GrantSpecialStatusEffect로 별도 부여합니다.")]
    public BuffClassificationDefinition buffClassification;
    [Header("Additional Classification Rules")]
    [InspectorName("Also Harmful")]
    [Tooltip("공유 분류에 더해 해로운 버프로 분류합니다.")]
    public bool harmful;
    [InspectorName("Allow Cleanse")]
    [Tooltip("끄면 Cleanse로 제거할 수 없습니다. 공유 분류의 Dispellable이 꺼져 있어도 보호됩니다.")]
    public bool dispellable = true;
    [InspectorName("Additional Cleanse Tags")]
    [Tooltip("공유 분류 태그와 함께 적용합니다. 04.Items/Buffs/Classification/Tag에서 태그를 만드세요.")]
    public BuffFlagDefinition[] flags;

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

    public bool HasRuntimePayload()
    {
        if (HasValidModifier() || harmful || buffClassification != null)
            return true;
        if (flags != null)
            foreach (BuffFlagDefinition flag in flags)
                if (flag != null) return true;
        return false;
    }
}
