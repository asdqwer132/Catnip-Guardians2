using UnityEngine;

public class ItemEffectContext
{
    public GameObject owner;

    public ItemData sourceItemData;
    public EquipmentBag sourceBag;
    public ItemEffectData currentEffectData;

    public BuffManager buffManager;

    public Vector3 usePosition;
    public Vector3 targetPosition;

    public ItemEffectContext(
        GameObject owner,
        ItemData sourceItemData,
        Vector3 usePosition,
        Vector3 targetPosition,
        EquipmentBag sourceBag,
        ItemEffectData currentEffectData = null,
        BuffManager buffManager = null
    )
    {
        this.owner = owner;
        this.sourceItemData = sourceItemData;

        this.usePosition = usePosition;
        this.targetPosition = targetPosition;

        this.sourceBag = sourceBag;
        this.currentEffectData = currentEffectData;
        this.buffManager = buffManager;
    }

    public void SetCurrentEffect(ItemEffectData effectData)
    {
        currentEffectData = effectData;
    }
}