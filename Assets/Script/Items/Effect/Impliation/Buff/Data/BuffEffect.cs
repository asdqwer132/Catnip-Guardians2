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

    [Header("Classification")]
    [Tooltip("해로운 버프로 분류합니다. StatusDefinition의 Harmful이 켜져 있어도 해로운 버프로 취급합니다.")]
    public bool harmful;
    [Tooltip("끄면 Cleanse로 제거할 수 없습니다. StatusDefinition의 Dispellable이 꺼져 있어도 정화가 차단됩니다.")]
    public bool dispellable = true;
    [Tooltip("정화 조건 등에 사용할 확장 플래그입니다. GameData/Items/Buff/Flag에서 플래그 에셋을 만들어 연결합니다.")]
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
        if (HasValidModifier() || harmful || (buffInfo != null && buffInfo.statusDefinition != null))
            return true;
        if (flags != null)
            foreach (BuffFlagDefinition flag in flags)
                if (flag != null) return true;
        return false;
    }
}
