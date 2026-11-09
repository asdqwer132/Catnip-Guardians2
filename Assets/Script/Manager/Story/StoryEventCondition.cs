using System;
using UnityEngine;

public enum StoryComparisonOperator { TextEquals, NumberAtLeast, NumberAtMost }

[Serializable]
public sealed class StoryEventComparison
{
    [Tooltip("Payload field name, such as plant-id, item-id, map-id, node-id, amount or growth.")]
    public string valueKey;
    public StoryComparisonOperator comparison;
    public string textValue;
    public double numberValue;

    public bool Matches(StoryProgressEvent fact)
    {
        if (string.IsNullOrWhiteSpace(valueKey)) return false;
        if (comparison == StoryComparisonOperator.TextEquals)
            return fact.TryGetText(valueKey, out var text) && string.Equals(text, textValue, StringComparison.Ordinal);
        if (!fact.TryGetNumber(valueKey, out double number) || double.IsNaN(numberValue) || double.IsInfinity(numberValue)) return false;
        switch (comparison)
        {
            case StoryComparisonOperator.NumberAtLeast: return number >= numberValue;
            case StoryComparisonOperator.NumberAtMost: return number <= numberValue;
            default: return false;
        }
    }
}

[CreateAssetMenu(menuName = "Story/Progression/Event Condition")]
public sealed class StoryEventCondition : StoryProgressCondition
{
    public string eventId;
    [Tooltip("Every comparison must pass on the same event. An empty list accepts any payload for this event ID.")]
    public StoryEventComparison[] comparisons = Array.Empty<StoryEventComparison>();

    public override bool Matches(StoryProgressEvent fact)
    {
        if (string.IsNullOrWhiteSpace(eventId) || !string.Equals(fact.EventId, eventId, StringComparison.Ordinal)) return false;
        if (comparisons != null)
            foreach (var test in comparisons)
                if (test == null || !test.Matches(fact)) return false;
        return true;
    }
}
