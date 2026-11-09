using UnityEngine;

[CreateAssetMenu(fileName = "MainPlantIndexEffect", menuName = "04.Items/Effects/Direct Effects/Main Plant Index")]
public sealed class MainPlantIndexEffect : ItemEffectData
{
    [Min(0)] public int plantIndex;
    public override void ExecuteEffect(ItemEffectContext context)
    {
        var plants = PlantManager.instance;
        if (plants == null || context == null) return;
        if (context.origin == ItemEffectOrigin.Skill) plants.SetSkillPlantIndex(plantIndex);
        else plants.SetPotionPlantIndex(plantIndex);
    }
}
