using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class SummonModificationSettings
{
    [Tooltip("0이면 해당 소환물이 제거될 때까지 유지합니다.")]
    [Min(0f)] public float duration;
    [Tooltip("0이면 횟수 제한 없음. 실제 공격이 성공했을 때만 차감합니다.")]
    [Min(0)] public int attackCount;
    [Min(0f)] public float damageMultiplier = 1f;
    [Min(0f)] public float healingMultiplier = 1f;
    public bool replaceAttackItem;
    public ItemData attackItem;
    public bool replaceModules;
    public SummonBehaviourModule[] modules;
    [Tooltip("이 효과 적용 시 현재 남은 수명에 더합니다. 일반 수명 버프는 기존 BuffEffect를 사용합니다.")]
    public float addRemainingLifetime;
}

[CreateAssetMenu(fileName = "SummonModifyEffect", menuName = "GameData/Items/Effects/Summon/Modify")]
public sealed class SummonModifyEffect : ItemEffectData
{
    public SummonSelection selection = new SummonSelection();
    public SummonModificationSettings modification = new SummonModificationSettings();
    [Tooltip("수치 버프 또는 상태 키를 선택된 소환물에 등록합니다.")]
    public BuffEffect[] buffs;

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || !context.CanContinue) return;
        var targets = new List<SummonItemThrower>();
        SummonRegistry.Collect(selection, context, targets);
        foreach (SummonItemThrower summon in targets)
        {
            if (!context.CanContinue) break;
            summon.AddModification(modification, context);
            if (context.buffManager == null || buffs == null) continue;
            foreach (BuffEffect buff in buffs)
                if (buff != null) context.buffManager.RegisterBuffForTarget(buff, context, summon);
        }
    }
}

// 설정을 복사하고 소환물의 생명 번호에 귀속한다. 겹친 프로필은 마지막 유효 설정이 우선한다.
public sealed class SummonModification : IDisposable
{
    public readonly bool ReplaceAttackItem, ReplaceModules;
    public readonly ItemData AttackItem;
    public readonly SummonBehaviourModule[] Modules;
    public readonly float DamageMultiplier, HealingMultiplier;
    private readonly int lifeId;
    private readonly ItemEffectContext context;
    private readonly bool timed, counted;
    private float remaining;
    private int attacks;
    private ItemEffectLease lease;
    private bool disposed;

    internal SummonModification(SummonItemThrower summon, SummonModificationSettings settings, ItemEffectContext source)
    {
        lifeId = summon.LifeId;
        context = source != null ? source.Copy(summon.transform.position, source.direction) : null;
        timed = settings.duration > 0f;
        remaining = EffectStatUtility.Safe(settings.duration, 0f, 600f, 0f) *
            (source != null ? EffectStatUtility.Safe(source.durationMultiplier, 0f, 100f, 1f) : 1f);
        counted = settings.attackCount > 0;
        attacks = Mathf.Clamp(settings.attackCount, 0, 100000);
        ReplaceAttackItem = settings.replaceAttackItem;
        AttackItem = settings.attackItem;
        ReplaceModules = settings.replaceModules;
        Modules = settings.modules != null ? (SummonBehaviourModule[])settings.modules.Clone() : new SummonBehaviourModule[0];
        DamageMultiplier = EffectStatUtility.Safe(settings.damageMultiplier, 0f, 1000f, 1f);
        HealingMultiplier = EffectStatUtility.Safe(settings.healingMultiplier, 0f, 1000f, 1f);
        lease = context != null ? context.RetainLifetime() : null;
    }

    internal bool IsValid(SummonItemThrower summon) => !disposed && summon.LifeId == lifeId &&
        (context == null || context.CanContinue) && (!timed || remaining > 0f) && (!counted || attacks > 0);
    internal bool HasCompletedNaturally(SummonItemThrower summon) => !disposed && summon.LifeId == lifeId &&
        (context == null || context.CanContinue) && ((timed && remaining <= 0f) || (counted && attacks <= 0));
    internal void Tick(float deltaTime) { if (timed) remaining -= deltaTime; }
    internal void ConsumeAttack() { if (counted) attacks = Mathf.Max(0, attacks - 1); }
    public void Dispose() => Dispose(false);
    internal void Dispose(bool completed)
    {
        if (disposed) return;
        disposed = true;
        ItemEffectLease ended = lease;
        lease = null;
        if (ended == null) return;
        if (completed && (context == null || context.CanContinue)) ended.Finish(); else ended.Cancel();
    }
}
