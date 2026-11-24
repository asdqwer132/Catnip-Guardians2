using System;

// Immutable snapshots: rules receive IDs and values, never scene objects or data asset types.
public readonly struct StoryProgressValue
{
    public string Key { get; }
    public string Text { get; }
    public double Number { get; }
    public bool IsNumber { get; }
    private StoryProgressValue(string key, string text, double number, bool isNumber)
    { Key = key; Text = text; Number = number; IsNumber = isNumber; }
    public static StoryProgressValue TextValue(string key, string value) => new StoryProgressValue(key, value, 0, false);
    public static StoryProgressValue NumberValue(string key, double value) => new StoryProgressValue(key, null, value, true);
}

public readonly struct StoryProgressEvent
{
    public string EventId { get; }
    private readonly StoryProgressValue[] values;
    public StoryProgressEvent(string eventId, params StoryProgressValue[] values)
    {
        EventId = eventId;
        this.values = values == null ? Array.Empty<StoryProgressValue>() : (StoryProgressValue[])values.Clone();
    }
    public bool TryGetText(string key, out string value)
    {
        if (values != null)
            foreach (var entry in values)
                if (entry.Key == key && !entry.IsNumber && entry.Text != null) { value = entry.Text; return true; }
        value = null;
        return false;
    }
    public bool TryGetNumber(string key, out double value)
    {
        if (values != null)
            foreach (var entry in values)
                if (entry.Key == key && entry.IsNumber && !double.IsNaN(entry.Number) && !double.IsInfinity(entry.Number))
                { value = entry.Number; return true; }
        value = 0;
        return false;
    }
}

// Defaults only. Each publisher exposes its event ID in the Inspector.
public static class StoryProgressEventIds
{
    public const string PlantGrowthCompleted = "plant.growth-completed";
    public const string PotionCrafted = "potion.crafted";
    public const string SkillUnlocked = "skill.unlocked";
    public const string ChestItemObtained = "chest.item-obtained";
}
