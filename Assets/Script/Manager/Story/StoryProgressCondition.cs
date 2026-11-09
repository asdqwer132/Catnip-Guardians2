using UnityEngine;

public enum StoryProgressEventKind { PlantGrown, PotionCrafted, SkillUnlocked, ChestItemObtained, Custom }

public readonly struct StoryProgressEvent
{
    public readonly StoryProgressEventKind Kind;
    public readonly Object Target;
    public readonly string EventId;
    public StoryProgressEvent(StoryProgressEventKind kind, Object target = null, string eventId = null)
    { Kind = kind; Target = target; EventId = eventId; }
}

// Add another condition asset subclass to support new gameplay conditions.
public abstract class StoryProgressCondition : ScriptableObject
{
    public abstract bool Matches(StoryProgressEvent fact);
}
