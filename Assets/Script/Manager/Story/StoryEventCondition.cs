using UnityEngine;

[CreateAssetMenu(menuName = "Story/Progression/Event Condition")]
public sealed class StoryEventCondition : StoryProgressCondition
{
    public StoryProgressEventKind eventKind;
    [Header("Plant Grown")]
    public PlantData plant;
    [Header("Potion Crafted / Chest Item Obtained")]
    public ItemData item;
    [Header("Skill Unlocked (both must match)")]
    public SkillMapData skillMap;
    public SkillNodeData skillNode;
    [Header("Custom Event")]
    public string eventId;

    public override bool Matches(StoryProgressEvent fact)
    {
        if (fact.Kind != eventKind) return false;
        switch (eventKind)
        {
            case StoryProgressEventKind.PlantGrown: return plant != null && fact.Target == plant;
            case StoryProgressEventKind.PotionCrafted:
            case StoryProgressEventKind.ChestItemObtained: return item != null && fact.Target == item;
            case StoryProgressEventKind.SkillUnlocked:
                return skillMap != null && skillNode != null && fact.Target == skillNode && skillNode.skillMap == skillMap;
            case StoryProgressEventKind.Custom: return !string.IsNullOrWhiteSpace(eventId) && eventId == fact.EventId;
            default: return false;
        }
    }
}
