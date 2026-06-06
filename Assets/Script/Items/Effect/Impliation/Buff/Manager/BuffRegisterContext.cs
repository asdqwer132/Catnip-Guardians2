using UnityEngine;

public class BuffRegisterContext
{
    public GameObject owner;
    public ItemData sourceItemData;
    public EquipmentBag sourceBag;
    public ItemEffectData sourceEffectData;
    public BuffManager buffManager;
    public Vector3 usePosition;
    public Vector3 targetPosition;

    public BuffRegisterContext(ItemEffectContext context, BuffManager buffManager)
    {
        if (context == null)
            return;

        owner = context.owner;
        sourceItemData = context.sourceItemData;
        sourceBag = context.sourceBag;
        sourceEffectData = context.currentEffectData;
        this.buffManager = buffManager;
        usePosition = context.usePosition;
        targetPosition = context.targetPosition;
    }
}
