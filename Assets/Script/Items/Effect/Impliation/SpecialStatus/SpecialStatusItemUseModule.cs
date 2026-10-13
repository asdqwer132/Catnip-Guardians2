using System;
using UnityEngine;

// 공유 에셋은 설정만 보관합니다. 사용 사건과 난수는 호출마다 전달합니다.
public abstract class SpecialStatusItemUseModule : ScriptableObject
{
    [Header("아이템 사용 조건")]
    [Tooltip("비우면 모든 시리즈. 예: Mineral만 넣으면 광물 아이템에만 반응합니다.")]
    public ItemSeries[] allowedSeries;
    [Tooltip("비우면 모든 아이템. 시리즈 조건과 함께 설정하면 둘 다 만족해야 합니다.")]
    public ItemData[] allowedItems;
    public ItemData[] excludedItems;
    [Tooltip("별도 에셋이어도 이 Data ID면 제외합니다. 비우면 에셋 참조만 검사합니다.")]
    public string[] excludedItemIds;
    [Tooltip("0.1 = 10%, 1 = 100%. 상태 중첩 수와 무관하게 모듈마다 한 번 판정합니다.")]
    [Range(0f, 1f)] public float probability = 1f;

    public bool CanReact(ItemEffectContext context, ItemThrowExecutor throwExecutor = null)
    {
        if (context == null || !context.CanContinue || !context.allowItemUseReactions ||
            context.sourceItemData == null || EffectStatUtility.Safe(probability, 0f, 1f, 0f) <= 0f)
            return false;
        ItemData item = context.sourceItemData;
        if (allowedSeries != null && allowedSeries.Length > 0 && Array.IndexOf(allowedSeries, item.series) < 0) return false;
        if (allowedItems != null && allowedItems.Length > 0 && Array.IndexOf(allowedItems, item) < 0) return false;
        if (excludedItems != null && Array.IndexOf(excludedItems, item) >= 0) return false;
        if (!string.IsNullOrEmpty(item.dataId) && excludedItemIds != null && Array.IndexOf(excludedItemIds, item.dataId) >= 0) return false;
        return CanPerform(context, throwExecutor);
    }

    public bool TryReact(ItemEffectContext context, ItemThrowExecutor throwExecutor = null, Func<float> randomValue = null)
    {
        if (!CanReact(context, throwExecutor)) return false;
        float chance = EffectStatUtility.Safe(probability, 0f, 1f, 0f);
        // Unity Random.value는 1도 반환할 수 있습니다. 100%는 난수와 관계없이 반드시 성공합니다.
        if (chance < 1f && !PassesChance(chance, randomValue != null ? randomValue() : UnityEngine.Random.value)) return false;
        return context.CanContinue && Perform(context, throwExecutor);
    }

    public static bool PassesChance(float probability, float randomValue)
    {
        float chance = EffectStatUtility.Safe(probability, 0f, 1f, 0f);
        if (chance >= 1f) return true;
        return chance > 0f && !float.IsNaN(randomValue) && !float.IsInfinity(randomValue) &&
            randomValue >= 0f && randomValue < chance;
    }

    protected static ItemEffectContext ReactionContext(ItemEffectContext source, bool consumeUseBuffs = false)
    {
        ItemEffectContext child = source.Copy(source.targetPosition, source.direction);
        // 비행·소환·종료 후 아이템까지 이어져도 추가 상태 반응이 다시 발생하지 않습니다.
        child.allowItemUseReactions = false;
        child.consumeUseBuffs = consumeUseBuffs && source.consumeUseBuffs;
        return child;
    }

    protected abstract bool CanPerform(ItemEffectContext context, ItemThrowExecutor throwExecutor);
    protected abstract bool Perform(ItemEffectContext context, ItemThrowExecutor throwExecutor);
}
