using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class WeightedEffectEntry
{
    [Min(0f)] public float weight = 1f;
    public ItemData item;
    public ItemEffectData[] effects;
    public bool HasPayload
    {
        get
        {
            if (item != null) return true;
            if (effects != null) foreach (ItemEffectData effect in effects) if (effect != null) return true;
            return false;
        }
    }
}

[CreateAssetMenu(fileName = "WeightedRandomEffect", menuName = "GameData/Items/Effects/Weighted Random")]
public sealed class WeightedRandomEffect : ItemEffectData
{
    public WeightedEffectEntry[] entries;
    [Tooltip("전체 결과를 먼저 뽑은 뒤 같은 프레임에 실행합니다. 상자는 5로 설정합니다.")]
    [Range(1, 128)] public int selectionCount = 1;
    public bool allowDuplicates = true;
    [Tooltip("자동 사용이 사용 횟수 버프를 소비할지 설정합니다. 재고와 슬롯은 소비하지 않습니다.")]
    public bool consumeUseBuffs;
    public bool triggerSpecialItems;
    public override void ExecuteEffect(ItemEffectContext context)
    {
        List<WeightedEffectEntry> selected = SelectEntries(UnityEngine.Random.value);
        foreach (WeightedEffectEntry entry in selected)
        {
            if (!context.CanContinue) break;
            ItemEffectContext child = context.Copy(context.targetPosition, context.direction);
            if (entry.item != null)
                ItemEffectExecutor.ExecuteItem(entry.item, child.usePosition, child.targetPosition, child.direction,
                    child.owner, child.sourceBag, child.buffManager, child, triggerSpecialItems,
                    consumeUseBuffs: consumeUseBuffs);
            ItemEffectUtility.Execute(entry.effects, child);
        }
    }
    public List<WeightedEffectEntry> SelectEntries(float firstRoll)
    {
        List<WeightedEffectEntry> pool = new List<WeightedEffectEntry>();
        if (entries != null)
            foreach (WeightedEffectEntry entry in entries)
                if (entry != null && entry.HasPayload && entry.weight > 0f &&
                    !float.IsNaN(entry.weight) && !float.IsInfinity(entry.weight))
                    pool.Add(new WeightedEffectEntry { weight = entry.weight, item = entry.item,
                        effects = ItemEffectUtility.Copy(entry.effects) });
        List<WeightedEffectEntry> selected = new List<WeightedEffectEntry>();
        for (int i = 0; i < Mathf.Clamp(selectionCount, 1, 128) && pool.Count > 0; i++)
        {
            double sum = 0d;
            foreach (WeightedEffectEntry entry in pool) sum += entry.weight;
            float sample = i == 0 ? firstRoll : UnityEngine.Random.value;
            if (float.IsNaN(sample) || float.IsInfinity(sample)) sample = 0f;
            double roll = Mathf.Clamp01(sample) * sum;
            int index = pool.Count - 1;
            for (int j = 0; j < pool.Count; j++)
            {
                roll -= pool[j].weight;
                if (roll < 0d) { index = j; break; }
            }
            selected.Add(pool[index]);
            if (!allowDuplicates) pool.RemoveAt(index);
        }
        return selected;
    }
}
