using UnityEngine;

public enum BuffTarget
{
    Self,
    Bag,
    Item,
    All
}

[CreateAssetMenu(
    fileName = "BuffEffect",
    menuName = "Game/Item Effect/Buff"
)]
public class BuffEffect : ItemEffectData
{
    [Header("Buff Target")]
    public BuffTarget targetScope = BuffTarget.Bag;

    [Tooltip("SameBag / All 버프일 때, 버프를 발생시킨 아이템 자신도 이 버프를 받을지")]
    public bool includeSelf = false;

    [Header("UI")]
    [Tooltip("이 버프를 버프 UI에 표시할지")]
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
}