using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class StoryProgressionTests
{
    private readonly List<Object> assets = new List<Object>();
    private double now;
    private T Asset<T>() where T : ScriptableObject
    { var value = ScriptableObject.CreateInstance<T>(); assets.Add(value); return value; }
    private StoryProgressRule Rule(string id, StoryProgressStartMode mode = StoryProgressStartMode.Confirmation)
    {
        var rule = Asset<StoryProgressRule>(); rule.id = id; rule.story = Asset<StoryData>();
        rule.startMode = mode; rule.delaySeconds = 5; return rule;
    }
    private StoryProgressTracker Tracker(params StoryProgressRule[] rules) => new StoryProgressTracker(rules, clock: () => now);
    [TearDown] public void Cleanup() { foreach (var value in assets) Object.DestroyImmediate(value); assets.Clear(); now = 0; }

    [Test]
    public void AuthoredRuleTemplatesUseTheirOwnConfirmationAreasAndRemainInactiveUntilTargetsAreAssigned()
    {
        string[] names = { "PlantGrowth", "PotionCraft", "SkillUnlock", "ChestItem" };
        string[] areas = { StoryProgressManager.BattleResult, StoryProgressManager.PotionResult, "", StoryProgressManager.ChestResult };
        var ids = new HashSet<string>();
        for (int i = 0; i < names.Length; i++)
        {
            var rule = AssetDatabase.LoadAssetAtPath<StoryProgressRule>("Assets/Data/Scriptable/Story/Progression/Rules/" + names[i] + "Story.asset");
            Assert.That(rule, Is.Not.Null);
            Assert.That(ids.Add(rule.id), Is.True);
            var condition = rule.condition as StoryEventCondition;
            Assert.That(condition, Is.Not.Null);
            Assert.That(condition.eventKind, Is.EqualTo((StoryProgressEventKind)i));
            Assert.That(condition.Matches(new StoryProgressEvent(condition.eventKind)), Is.False);
            Assert.That(rule.confirmationArea, Is.EqualTo(areas[i]));
            Assert.That(rule.startMode, Is.EqualTo(i == 2 ? StoryProgressStartMode.Delay : StoryProgressStartMode.Confirmation));
        }
    }

    [Test]
    public void IndependentStoriesDoNotNeedEarlierRulesOrAdvanceOnAnUnrelatedConfirmation()
    {
        var first = Rule("earlier"); var later = Rule("later"); later.confirmationArea = StoryProgressManager.PotionResult;
        var tracker = Tracker(first, later);
        Assert.That(tracker.Confirm(StoryProgressManager.PotionResult), Is.False, "A click before the condition does not count.");
        tracker.MeetCondition(later.id);
        Assert.That(tracker.NextReady(), Is.Null);
        tracker.Confirm(StoryProgressManager.BattleResult);
        Assert.That(tracker.NextReady(), Is.Null);
        tracker.Confirm(StoryProgressManager.PotionResult);
        Assert.That(tracker.NextReady(), Is.SameAs(later));
        Assert.That(tracker.IsConditionMet(first.id), Is.False);
    }

    [Test]
    public void DelayStartsAtTheFirstMatchingFactAndDoesNotResetOnRepeatedEvents()
    {
        var rule = Rule("delayed", StoryProgressStartMode.Delay);
        var condition = Asset<StoryEventCondition>(); condition.eventKind = StoryProgressEventKind.Custom; condition.eventId = "gate";
        rule.condition = condition;
        var tracker = Tracker(rule);
        tracker.Report(new StoryProgressEvent(StoryProgressEventKind.Custom, eventId: "wrong"));
        Assert.That(tracker.IsConditionMet(rule.id), Is.False);
        now = 10; tracker.Report(new StoryProgressEvent(StoryProgressEventKind.Custom, eventId: "gate"));
        now = 14; Assert.That(tracker.NextReady(), Is.Null);
        Assert.That(tracker.Report(new StoryProgressEvent(StoryProgressEventKind.Custom, eventId: "gate")), Is.False);
        now = 15; Assert.That(tracker.NextReady(), Is.SameAs(rule));
    }

    [Test]
    public void MultipleReadyStoriesRemainQueuedUntilEachCompletesAndSurviveReload()
    {
        var a = Rule("a"); var b = Rule("b"); var tracker = Tracker(a, b);
        tracker.MeetCondition(a.id); tracker.MeetCondition(b.id); tracker.Confirm(a.confirmationArea);
        var reloaded = new StoryProgressTracker(new[] { a, b }, tracker.ToJson(), () => now);
        Assert.That(reloaded.NextReady(), Is.SameAs(a));
        reloaded.Complete(a.id);
        Assert.That(reloaded.NextReady(), Is.SameAs(b));
        reloaded.Complete(b.id);
        var finished = new StoryProgressTracker(new[] { a, b }, reloaded.ToJson(), () => now);
        Assert.That(finished.NextReady(), Is.Null);
        Assert.That(finished.MeetCondition(a.id), Is.False);
        Assert.That(finished.IsCompleted(b.id), Is.True);
    }

    [Test]
    public void UnconfirmedConditionAndDelayedDeadlineSurviveRestart()
    {
        var confirmation = Rule("confirmation"); var delay = Rule("delay", StoryProgressStartMode.Delay);
        var tracker = Tracker(confirmation, delay); now = 100;
        tracker.MeetCondition(confirmation.id); tracker.MeetCondition(delay.id);
        now = 104; var reloaded = new StoryProgressTracker(new[] { confirmation, delay }, tracker.ToJson(), () => now);
        Assert.That(reloaded.IsConditionMet(confirmation.id), Is.True);
        Assert.That(reloaded.NextReady(), Is.Null);
        now = 105; Assert.That(reloaded.NextReady(), Is.SameAs(delay));
    }

    [TestCase(StoryProgressEventKind.PlantGrown)]
    [TestCase(StoryProgressEventKind.PotionCrafted)]
    [TestCase(StoryProgressEventKind.ChestItemObtained)]
    public void ConfiguredAssetAndEventKindMustBothMatch(StoryProgressEventKind kind)
    {
        var condition = Asset<StoryEventCondition>(); condition.eventKind = kind;
        Object target;
        if (kind == StoryProgressEventKind.PlantGrown) { condition.plant = Asset<PlantData>(); target = condition.plant; }
        else { condition.item = Asset<ItemData>(); target = condition.item; }
        Assert.That(condition.Matches(new StoryProgressEvent(kind)), Is.False);
        Assert.That(condition.Matches(new StoryProgressEvent(kind, target)), Is.True);
        Assert.That(condition.Matches(new StoryProgressEvent(StoryProgressEventKind.Custom, target)), Is.False);
    }

    [Test]
    public void SkillConditionRequiresTheSpecifiedMapAndNode()
    {
        var condition = Asset<StoryEventCondition>(); condition.eventKind = StoryProgressEventKind.SkillUnlocked;
        condition.skillMap = Asset<SkillMapData>(); condition.skillNode = Asset<SkillNodeData>();
        Assert.That(condition.Matches(new StoryProgressEvent(StoryProgressEventKind.SkillUnlocked, condition.skillNode)), Is.False);
        condition.skillNode.skillMap = condition.skillMap;
        Assert.That(condition.Matches(new StoryProgressEvent(StoryProgressEventKind.SkillUnlocked, condition.skillNode)), Is.True);
        Assert.That(condition.Matches(new StoryProgressEvent(StoryProgressEventKind.SkillUnlocked, Asset<SkillNodeData>())), Is.False);
    }

    [Test]
    public void AConditionWithoutAStoryDoesNotBlockAnotherReadyStory()
    {
        var invalid = Rule("unassigned"); invalid.story = null; var valid = Rule("valid");
        var tracker = Tracker(invalid, valid);
        tracker.MeetCondition(invalid.id); tracker.MeetCondition(valid.id); tracker.Confirm(valid.confirmationArea);
        Assert.That(tracker.NextReady(), Is.SameAs(valid));
    }
}
