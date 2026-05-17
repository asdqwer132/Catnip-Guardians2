using UnityEngine;

[CreateAssetMenu(fileName = "HealEffect", menuName = "Game/Item Effect/Heal")]
public class HealEffect : ItemEffectData
{
    public float healAmount = 10f;

    public override void Execute(ItemEffectContext context)
    {
        Health health = context.owner.GetComponent<Health>();

        if (health == null)
            return;

        health.Heal(healAmount);

        Debug.Log(context.itemData.itemName + " 회복 실행: " + healAmount);
    }
}