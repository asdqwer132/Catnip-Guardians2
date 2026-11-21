using UnityEngine;

[CreateAssetMenu(fileName = "MainPlantIndexEffect", menuName = "Items/Effects/Direct Effects/Main Plant Index")]
public sealed class MainPlantIndexEffect : ItemEffectData
{
    [Min(0)] public int plantIndex;
    public override void ExecuteEffect(ItemEffectContext context)
    {
        var plants = GameManager.instance?.plantManager;
        if (plants == null || context == null) return;
        if (context.origin == ItemEffectOrigin.Skill) plants.SetSkillPlantIndex(plantIndex);
        else plants.SetPotionPlantIndex(plantIndex);
    }
}
