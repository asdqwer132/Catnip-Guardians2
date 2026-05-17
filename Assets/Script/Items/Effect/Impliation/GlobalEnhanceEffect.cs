using UnityEngine;

[CreateAssetMenu(fileName = "GlobalEnhanceEffect", menuName = "Game/Item Effect/Global Enhance")]
public class GlobalEnhanceEffect : ItemEffectData
{
    public EffectStat bonus = new EffectStat();

    public override void Execute(ItemEffectContext context)
    {
        PlayerStat stat = context.owner.GetComponent<PlayerStat>();

        if (stat == null)
            return;

        stat.AddBonus(bonus);

        Debug.Log(context.itemData.itemName + " 전체 강화 실행");
    }
}