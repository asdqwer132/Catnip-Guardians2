using UnityEngine;

public enum BuffTarget
{
    Self,
    Bag,
    Item,
    All,

    EnemyInRange,

    // 지금 살아있는 적에게만 적용
    AllEnemies,

    // 지금 살아있는 적 + 이후 새로 스폰되는 적까지 적용
    AllEnemiesIncludingFuture
}

[CreateAssetMenu(
    fileName = "BuffEffect",
    menuName = "Game/Item Effect/Buff"
)]
public class BuffEffect : ItemEffectData
{
    [Header("Buff Target")]
    public BuffTarget targetScope = BuffTarget.Bag;

    [Tooltip("버프를 발생시킨 아이템 자신도 이 버프를 받을지")]
    public bool includeSelf = false;

    [Header("UI")]
    public bool showInUI = true;

    [Header("Buff Info")]
    public BuffInfo buffInfo = new BuffInfo();

    [Header("Buff Stat")]
    public BuffStat bonus = new BuffStat();

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null)
            return;

        if (context.buffManager == null)
        {
            Debug.LogWarning("ItemEffectContext에 BuffManager가 없습니다.");
            return;
        }

        context.buffManager.RegisterBuff(this, context);
    }

    public EnemyBuffStat GetEnemyBuffStat()
    {
        if (bonus == null)
            return null;

        return bonus.enemyBuffStat;
    }
}