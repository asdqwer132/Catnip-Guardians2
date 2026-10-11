using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

// 전체 버프는 아이템 계산과 생성된 오브젝트 계산 양쪽에 나타날 수 있습니다.
// 계산된 스탯의 복제본에 적용 출처를 이어서 같은 버프를 두 번 곱하지 않게 합니다.
public static class BuffStatCloneUtility
{
    private sealed class AppliedBuff
    {
        private readonly BuffStorage storage;
        private readonly ItemData item;
        private readonly EquipmentBag bag;
        private readonly ItemEffectData effect;

        public AppliedBuff(ActiveBuff buff)
        {
            storage = buff.StorageOwner;
            item = buff.sourceItemData;
            bag = buff.sourceBag;
            effect = buff.sourceEffectData;
        }

        // 재부여/제거 후 같은 출처가 다시 등록되어도 캡처된 Snapshot은 그대로 사용합니다.
        public bool Matches(ActiveBuff buff)
            => ReferenceEquals(storage, buff.StorageOwner) && ReferenceEquals(item, buff.sourceItemData) &&
               ReferenceEquals(bag, buff.sourceBag) && ReferenceEquals(effect, buff.sourceEffectData);
    }

    // 스탯이 더 이상 사용되지 않으면 기록도 GC 대상입니다. 에셋이나 씬에는 저장하지 않습니다.
    private static readonly ConditionalWeakTable<object, List<AppliedBuff>> history =
        new ConditionalWeakTable<object, List<AppliedBuff>>();

    public static T Clone<T>(T source) where T : class, IGameStat<T>
    {
        if (source == null) return null;
        T clone = source.Clone();
        List<AppliedBuff> applied;
        if (clone != null && !ReferenceEquals(source, clone) && history.TryGetValue(source, out applied))
        {
            history.Remove(clone);
            history.Add(clone, new List<AppliedBuff>(applied));
        }
        return clone;
    }

    internal static bool WasApplied(object stat, ActiveBuff buff)
    {
        if (stat == null || buff == null || buff.target == null || buff.target.kind != BuffTargetKind.AllTargets)
            return false;
        List<AppliedBuff> applied;
        if (!history.TryGetValue(stat, out applied)) return false;
        for (int i = 0; i < applied.Count; i++)
            if (applied[i].Matches(buff)) return true;
        return false;
    }

    internal static void RecordApplied(object stat, ActiveBuff buff)
    {
        if (stat == null || buff == null || buff.target == null ||
            buff.target.kind != BuffTargetKind.AllTargets || WasApplied(stat, buff)) return;
        history.GetValue(stat, CreateHistory).Add(new AppliedBuff(buff));
    }

    private static List<AppliedBuff> CreateHistory(object stat) => new List<AppliedBuff>();
}
