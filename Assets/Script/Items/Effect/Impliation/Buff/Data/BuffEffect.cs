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
    AllEnemiesIncludingFuture,

    // enemy spawner
    EnemySpawner,
    AllEnemySpawners
}

/// <summary>
/// BuffEffect
/// 
/// 역할:
/// - 아이템 효과 중 "버프를 등록하는 효과"를 의미한다.
/// - 실제 계산은 하지 않고, BuffManager에게 등록 요청만 보낸다.
/// - 어떤 대상에게 버프를 줄지는 targetScope가 결정한다.
/// 
/// 주의:
/// - 이 클래스는 실행 트리거 역할이다.
/// - 버프 저장/중첩/계산은 BuffRegistrar, ActiveBuff, BuffStatCalculator가 담당한다.
/// </summary>
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

    [Header("Enemy Spawner Target")]
    public EnemySpawner targetEnemySpawner;

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

    public EnemySpawnerBuffStat GetEnemySpawnerBuffStat()
    {
        if (bonus == null)
            return null;

        return bonus.enemySpawnerBuffStat;
    }
}