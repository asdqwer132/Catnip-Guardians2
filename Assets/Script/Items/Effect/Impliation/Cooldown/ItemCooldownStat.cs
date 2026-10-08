using System;

[Serializable]
public sealed class ItemCooldownStat : IGameStat<ItemCooldownStat>
{
    public float cooldown;
    public float cooldownRecoveryRate = 1f;

    public ItemCooldownStat Clone() => (ItemCooldownStat)MemberwiseClone();

    public void Clamp()
    {
        cooldown = EffectStatUtility.Safe(cooldown, 0f, 1000000f, 0f);
        cooldownRecoveryRate = EffectStatUtility.Safe(cooldownRecoveryRate, 0f, 100f, 1f);
    }

    // 준비 시간과 UI 조회는 아이템 사용이 아니므로 횟수 버프를 소비하지 않는다.
    public static ItemCooldownStat Resolve(ItemData item, EquipmentBag bag, BuffManager manager)
    {
        if (item == null)
            return new ItemCooldownStat();

        var stat = new ItemCooldownStat { cooldown = item.Cooldown };
        stat.Clamp();
        if (manager != null)
            stat = manager.GetBuffedStatForItem(stat, item, bag, BuffCalculationMode.All, false) ?? stat;
        stat.Clamp();
        return stat;
    }
}
