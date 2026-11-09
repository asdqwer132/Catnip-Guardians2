using System;
using System.Collections.Generic;
using UnityEngine;

// Tracks independent rules. List order only resolves ties; it never gates later stories.
public sealed class StoryProgressTracker
{
    [Serializable] private sealed class Entry
    {
        public string id;
        public bool conditionMet, confirmed, completed;
        public double readyAt;
    }
    [Serializable] private sealed class SaveData { public List<Entry> entries = new List<Entry>(); }
    private readonly List<StoryProgressRule> rules = new List<StoryProgressRule>();
    private readonly SaveData data;
    private readonly Func<double> clock;

    public StoryProgressTracker(IEnumerable<StoryProgressRule> configuredRules, string json = null, Func<double> clock = null)
    {
        this.clock = clock ?? (() => DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond);
        try { data = string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<SaveData>(json); }
        catch (ArgumentException) { Debug.LogWarning("Story progression save is invalid. Starting empty."); }
        if (data?.entries == null) data = new SaveData();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        if (configuredRules == null) return;
        foreach (var rule in configuredRules)
        {
            if (rule == null || string.IsNullOrWhiteSpace(rule.id)) continue;
            if (!ids.Add(rule.id))
            { Debug.LogWarning("Duplicate story progression rule ID: " + rule.id); continue; }
            rules.Add(rule);
        }
    }

    private Entry Get(string id)
    {
        var entry = data.entries.Find(value => value != null && value.id == id);
        if (entry == null) { entry = new Entry { id = id }; data.entries.Add(entry); }
        return entry;
    }
    public bool IsCompleted(string id) => Get(id).completed;
    public bool IsConditionMet(string id) => Get(id).conditionMet;
    public string ToJson() => JsonUtility.ToJson(data);

    public bool Report(StoryProgressEvent fact)
    {
        bool changed = false;
        foreach (var rule in rules)
            if (rule.condition != null && rule.condition.Matches(fact)) changed |= MeetCondition(rule.id);
        return changed;
    }
    public bool MeetCondition(string id)
    {
        var rule = rules.Find(value => value.id == id);
        if (rule == null) return false;
        var entry = Get(id);
        if (entry.conditionMet || entry.completed) return false;
        entry.conditionMet = true;
        entry.readyAt = clock() + Math.Max(0, rule.delaySeconds);
        return true;
    }
    public bool Confirm(string area)
    {
        if (string.IsNullOrWhiteSpace(area)) return false;
        bool changed = false;
        foreach (var rule in rules)
        {
            var entry = Get(rule.id);
            if (rule.startMode != StoryProgressStartMode.Confirmation || rule.confirmationArea != area
                || !entry.conditionMet || entry.confirmed || entry.completed) continue;
            entry.confirmed = true;
            changed = true;
        }
        return changed;
    }
    public StoryProgressRule NextReady()
    {
        foreach (var rule in rules)
        {
            var entry = Get(rule.id);
            if (rule.story == null || !entry.conditionMet || entry.completed) continue;
            if (rule.startMode == StoryProgressStartMode.Confirmation ? entry.confirmed : clock() >= entry.readyAt)
                return rule;
        }
        return null;
    }
    public void Complete(string id) => Get(id).completed = true;
}
