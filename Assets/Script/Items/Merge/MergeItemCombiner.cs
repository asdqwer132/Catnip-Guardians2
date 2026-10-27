using System.Collections.Generic;
using UnityEngine;

// 접촉 대기열과 레시피 합성만 담당합니다. 재료를 소비하거나 투입 주기를 변경하지 않습니다.
internal sealed class MergeItemCombiner
{
    private readonly MergegameManager owner;
    private readonly List<Contact> contacts = new List<Contact>();
    private readonly HashSet<ulong> queued = new HashSet<ulong>();
    private bool processing;
    private struct Contact { public MergeItemObject a, b; public int aLife, bLife; }
    public MergeItemCombiner(MergegameManager owner) => this.owner = owner;
    public void ClearPending() { contacts.Clear(); queued.Clear(); }

    public void QueueContact(MergeItemObject a, MergeItemObject b)
    {
        if (!owner.CanOperate || a == null || b == null || a == b || a.Owner != owner || b.Owner != owner || !a.CanMerge || !b.CanMerge) return;
        ulong key = a.Serial < b.Serial ? ((ulong)a.Serial << 32) | b.Serial : ((ulong)b.Serial << 32) | a.Serial;
        if (queued.Add(key)) contacts.Add(new Contact { a = a, b = b, aLife = a.LifeId, bLife = b.LifeId });
    }

    public void ProcessContacts()
    {
        if (processing) return;
        var pending = contacts.ToArray(); contacts.Clear(); queued.Clear(); processing = true;
        try
        {
            foreach (var contact in pending)
            {
                var a = contact.a; var b = contact.b;
                if (!owner.CanOperate) break;
                if (a == null || b == null || a.LifeId != contact.aLife || b.LifeId != contact.bLife ||
                    a.Owner != owner || b.Owner != owner || !a.CanMerge || !b.CanMerge) continue;
                TryMerge(a, b);
            }
        }
        finally { processing = false; }
    }

    private void TryMerge(MergeItemObject a, MergeItemObject b)
    {
        if (owner.recipeBook == null || !owner.recipeBook.TryGetRecipe(a.item, b.item, out var recipe)) return;
        var definition = owner.GetDefinitionAtMergeItem(recipe.resultItem);
        if (owner.objectPool == null || !MergegameManager.TryGetPrefab(definition, out _)) return;
        if (!a.Reserve()) return;
        if (!b.Reserve()) { a.Unreserve(); return; }
        int before = owner.Revision, aLife = a.LifeId, bLife = b.LifeId;
        MergeItemObject result = null;
        bool committed = false;
        try
        {
            Vector2 velocity = (a.Velocity * a.body.mass + b.Velocity * b.body.mass) / Mathf.Max(.001f, a.body.mass + b.body.mass);
            result = owner.SpawnPiece(definition, (a.transform.position + b.transform.position) * 0.5f);
            if (result == null || !result.IsLive || before != owner.Revision || !owner.CanOperate || a.LifeId != aLife || b.LifeId != bLife || !a.IsLive || !b.IsLive) return;
            a.ReturnToPool(); b.ReturnToPool();
            result.Velocity = velocity; result.SetSimulated(true); committed = true;

            owner.Report($"합성: {owner.GetItemDisplayName(recipe.resultItem)}");
        }
        finally
        {
            if (!committed && result != null) result.ReturnToPool();
            if (a != null && a.LifeId == aLife) a.Unreserve();
            if (b != null && b.LifeId == bLife) b.Unreserve();
        }
    }
}
