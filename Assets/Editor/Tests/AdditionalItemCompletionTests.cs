using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class AdditionalItemCompletionTests
{
    private readonly List<Object> owned = new List<Object>();
    private T Asset<T>() where T : ScriptableObject
    {
        T value = ScriptableObject.CreateInstance<T>(); owned.Add(value); return value;
    }
    private CompletionHoldingEffect Hold()
        => Asset<CompletionHoldingEffect>();
    private ItemData Item(params ItemEffectData[] effects)
    {
        ItemData item = Asset<ItemData>(); item.effectDatas = effects; return item;
    }
    private void Run(ItemData item, ItemEffectContext parent = null, GameObject owner = null)
        => ItemEffectExecutor.ExecuteItem(item, Vector3.zero, new Vector3(3f, 4f), Vector3.right,
            owner, null, null, parent, triggerSpecialItems: false);
    [TearDown] public void Cleanup()
    {
        foreach (Object value in owned) if (value != null) Object.DestroyImmediate(value);
        owned.Clear();
    }

    [Test] public void CompletionItemsWaitForAllEffectsWithoutRequiringAnEndVisual()
    {
        CompletionHoldingEffect first = Hold(), second = Hold();
        CombatEffectRecordingEffect next = Asset<CombatEffectRecordingEffect>();
        ItemData source = Item(first, second); source.afterCompletionItems = new[] { Item(next) };
        Run(source);
        Assert.That(next.calls, Is.Empty);
        first.Finish(); Assert.That(next.calls, Is.Empty);
        second.Finish(); Assert.That(next.calls.Count, Is.EqualTo(1));
        second.Finish(); Assert.That(next.calls.Count, Is.EqualTo(1));
    }

    [Test] public void FollowupUsesItsOwnItemDataAndTheCapturedCompletionList()
    {
        CompletionHoldingEffect hold = Hold();
        CombatEffectRecordingEffect original = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect replacement = Asset<CombatEffectRecordingEffect>();
        ItemData next = Item(original), source = Item(hold);
        source.afterCompletionItems = new[] { next };
        Run(source); source.afterCompletionItems[0] = Item(replacement);
        hold.Finish();
        Assert.That(original.calls.Count, Is.EqualTo(1));
        Assert.That(original.calls[0].sourceItemData, Is.SameAs(next));
        Assert.That(original.calls[0].targetPosition, Is.EqualTo(new Vector3(3f, 4f)));
        Assert.That(original.calls[0].direction, Is.EqualTo(Vector3.right));
        Assert.That(original.calls[0].consumeUseBuffs, Is.False);
        Assert.That(replacement.calls, Is.Empty);
    }

    [Test] public void OwnerPositionIsResolvedAtCompletionInsteadOfAtUse()
    {
        CompletionHoldingEffect hold = Hold();
        CombatEffectRecordingEffect next = Asset<CombatEffectRecordingEffect>();
        ItemData source = Item(hold); source.afterCompletionItems = new[] { Item(next) };
        source.afterCompletionItemsAtOwner = true;
        var owner = new GameObject("Completion Owner"); owned.Add(owner);
        Run(source, owner: owner); owner.transform.position = new Vector3(7f, 8f);
        hold.Finish();
        Assert.That(next.calls[0].targetPosition, Is.EqualTo(new Vector3(7f, 8f)));
        Assert.That(next.calls[0].usePosition, Is.EqualTo(new Vector3(7f, 8f)));
        Assert.That(next.calls[0].owner, Is.SameAs(owner));
    }

    [Test] public void CancellingOneChildSuppressesCompletionItems()
    {
        CompletionHoldingEffect first = Hold(), second = Hold();
        CombatEffectRecordingEffect next = Asset<CombatEffectRecordingEffect>();
        ItemData source = Item(first, second); source.afterCompletionItems = new[] { Item(next) };
        Run(source); first.Cancel(); second.Finish();
        Assert.That(next.calls, Is.Empty);
    }

    [Test] public void BattleResetSuppressesCompletionItems()
    {
        CompletionHoldingEffect hold = Hold();
        CombatEffectRecordingEffect next = Asset<CombatEffectRecordingEffect>();
        ItemData source = Item(hold); source.afterCompletionItems = new[] { Item(next) };
        Run(source); ItemEffectRuntime.CancelAll(); hold.Finish();
        Assert.That(next.calls, Is.Empty);
    }

    [Test] public void SelfAndIndirectCyclesAreSkippedBeforeUsingTheRepeatedItem()
    {
        CombatEffectRecordingEffect recordA = Asset<CombatEffectRecordingEffect>();
        CombatEffectRecordingEffect recordB = Asset<CombatEffectRecordingEffect>();
        ItemData a = Item(recordA), b = Item(recordB);
        a.afterCompletionItems = new[] { a, b }; b.afterCompletionItems = new[] { a };
        Run(a);
        Assert.That(recordA.calls.Count, Is.EqualTo(1));
        Assert.That(recordB.calls.Count, Is.EqualTo(1));
    }

    [Test] public void DuplicateSiblingItemsAreAllowedWhileNullAndEmptyItemsAreSkipped()
    {
        CombatEffectRecordingEffect next = Asset<CombatEffectRecordingEffect>(); ItemData target = Item(next);
        ItemData source = Item(Asset<CombatEffectRecordingEffect>());
        source.afterCompletionItems = new[] { null, Item(), target, target };
        Run(source);
        Assert.That(next.calls.Count, Is.EqualTo(2));
    }

    [Test] public void OuterLifetimeWaitsForChainedCompletionItems()
    {
        CompletionHoldingEffect holdB = Hold(), holdC = Hold();
        ItemData c = Item(holdC), b = Item(holdB), a = Item(Asset<CombatEffectRecordingEffect>());
        a.afterCompletionItems = new[] { b }; b.afterCompletionItems = new[] { c };
        int completions = 0;
        var outer = new ItemEffectLifetime(onCompleted: () => completions++, cancelOnChildFailure: true);
        var parent = new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null) { lifetime = outer };
        Run(a, parent); outer.Close();
        Assert.That(completions, Is.Zero);
        holdB.Finish(); Assert.That(completions, Is.Zero);
        holdC.Finish(); Assert.That(completions, Is.EqualTo(1));
    }
}

public sealed class CompletionHoldingEffect : ItemEffectData
{
    private ItemEffectLease lease;
    public override void ExecuteEffect(ItemEffectContext context) => lease = context.RetainLifetime();
    public void Finish()
    {
        ItemEffectLease held = lease; lease = null; if (held != null) held.Finish();
    }
    public void Cancel()
    {
        ItemEffectLease held = lease; lease = null; if (held != null) held.Cancel();
    }
}
