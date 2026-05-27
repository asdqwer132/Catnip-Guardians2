using UnityEngine;

public enum BuffTarget
{
    // item
    Self,
    Bag,
    Item,
    ItemSeries,
    All,

    // enemy
    EnemyInRange,
    AllEnemies,
    AllEnemiesIncludingFuture
}

[CreateAssetMenu(fileName = "BuffEffect", menuName = "Game/Item Effect/Buff")]
public class BuffEffect : ItemEffectData
{
    [Header("Buff Info")]
    public BuffTarget targetScope = BuffTarget.Bag;
    public BuffInfo buffInfo = new BuffInfo();
    public bool includeSelf = false;
    public bool showInUI = true;

    [Header("Item Target")]
    public ItemData targetItemData;

    [Header("Item Series Target")]
    public ItemSeries targetSeries = ItemSeries.None;

    [Header("Buff Stat")]
    public BuffStat bonus = new BuffStat();

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null)
            return;

        if (context.buffManager == null)
            return;

        context.buffManager.RegisterBuff(this, context);
    }

    public EnemyBuffStat GetEnemyBuffStat()
    {
        if (bonus == null)
            return null;

        return bonus.enemyBuffStat;
    }
}
