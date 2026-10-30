using System.Collections.Generic;
using UnityEngine;

// 접촉 대기열과 레시피 합성만 담당합니다. 투입 재고는 변경하지 않습니다.
internal sealed class MergeItemCombiner
{
    private readonly MergegameManager owner;
    private readonly List<Contact> contacts = new List<Contact>();
    private readonly HashSet<ulong> queued = new HashSet<ulong>();
    private bool processing;
    private struct Contact { public MergeItemObject a, b; public int aLife, bLife; }
    private sealed class RecipePlan
    {
        public ItemRecipeData recipe;
        public Dictionary<ItemData, int> required;
    }
    public MergeItemCombiner(MergegameManager owner) => this.owner = owner;
    public void ClearPending() { contacts.Clear(); queued.Clear(); }

    public void QueueContact(MergeItemObject a, MergeItemObject b)
    {
        if (!Usable(a) || !Usable(b) || a == b) return;
        ulong key = a.Serial < b.Serial ? ((ulong)a.Serial << 32) | b.Serial : ((ulong)b.Serial << 32) | a.Serial;
        if (queued.Add(key)) contacts.Add(new Contact { a = a, b = b, aLife = a.LifeId, bLife = b.LifeId });
    }
    private bool Usable(MergeItemObject item) => owner.CanOperate && item != null && item.Owner == owner && item.CanMerge;
    private bool Current(Contact contact)
        => Usable(contact.a) && Usable(contact.b) && contact.a.LifeId == contact.aLife && contact.b.LifeId == contact.bLife;

    public void ProcessContacts()
    {
        if (processing || contacts.Count == 0) return;
        var pending = contacts.ToArray(); ClearPending(); processing = true;
        try
        {
            var neighbors = new Dictionary<MergeItemObject, List<MergeItemObject>>();
            var lives = new Dictionary<MergeItemObject, int>();
            foreach (var contact in pending) if (Current(contact))
            {
                if (!neighbors.ContainsKey(contact.a)) neighbors.Add(contact.a, new List<MergeItemObject>());
                if (!neighbors.ContainsKey(contact.b)) neighbors.Add(contact.b, new List<MergeItemObject>());
                neighbors[contact.a].Add(contact.b); neighbors[contact.b].Add(contact.a);
                lives[contact.a] = contact.aLife; lives[contact.b] = contact.bLife;
            }
            var plans = new List<RecipePlan>();
            if (owner.recipeBook?.tiers != null) foreach (var tier in owner.recipeBook.tiers)
                if (tier?.itemRecipeDatas != null) foreach (var recipe in tier.itemRecipeDatas)
                    if (MergeTree.TryGetIngredients(recipe, out var required)) plans.Add(new RecipePlan { recipe = recipe, required = required });
            foreach (var contact in pending)
            {
                if (!owner.CanOperate) break;
                if (!Current(contact)) continue;
                foreach (var plan in plans)
                {
                    var selected = SelectIngredients(contact, plan.required, neighbors, lives);
                    if (selected != null && TryMerge(plan.recipe, selected)) break;
                }
            }
        }
        finally { processing = false; }
    }
    // 같은 접촉 묶음 안에서만 필요한 개수를 고릅니다. 멀리 떨어진 재료는 소비하지 않습니다.
    private List<MergeItemObject> SelectIngredients(Contact contact, Dictionary<ItemData, int> required,
        Dictionary<MergeItemObject, List<MergeItemObject>> neighbors, Dictionary<MergeItemObject, int> lives)
    {
        if (!required.ContainsKey(contact.a.item)) return null;
        var remaining = new Dictionary<ItemData, int>(required);
        var selected = new List<MergeItemObject>();
        var visited = new HashSet<MergeItemObject>();
        var search = new Queue<MergeItemObject>(); search.Enqueue(contact.a);
        while (search.Count > 0)
        {
            var item = search.Dequeue();
            if (!visited.Add(item) || !Usable(item) || item.LifeId != lives[item] || !required.ContainsKey(item.item)) continue;
            if (remaining[item.item] > 0) { selected.Add(item); remaining[item.item]--; }
            foreach (var next in neighbors[item]) search.Enqueue(next);
        }
        foreach (int amount in remaining.Values) if (amount > 0) return null;
        return selected;
    }
    private bool TryMerge(ItemRecipeData recipe, List<MergeItemObject> ingredients)
    {
        var definition = owner.GetDefinitionAtMergeItem(recipe.resultItem);
        if (owner.objectPool == null || !MergegameManager.TryGetPrefab(definition, out _)) return false;
        var lives = new List<int>();
        int before = owner.Revision;
        MergeItemObject result = null;
        bool committed = false;
        try
        {
            Vector3 position = Vector3.zero; Vector2 momentum = Vector2.zero; float mass = 0;
            foreach (var item in ingredients)
            {
                if (!item.Reserve()) return false;
                lives.Add(item.LifeId);
                position += item.transform.position; momentum += item.Velocity * item.body.mass; mass += item.body.mass;
            }
            result = owner.SpawnPiece(definition, position / ingredients.Count);
            if (result == null || !result.IsLive || before != owner.Revision || !owner.CanOperate) return false;
            for (int i = 0; i < ingredients.Count; i++)
                if (ingredients[i] == null || ingredients[i].LifeId != lives[i] || !ingredients[i].IsLive) return false;
            foreach (var item in ingredients) item.ReturnToPool();
            result.Velocity = momentum / Mathf.Max(.001f, mass); result.SetSimulated(true); committed = true;
            if (owner.discoveryHistory != null) owner.discoveryHistory.RecordCrafted(recipe.resultItem);
            owner.Report($"합성: {owner.GetItemDisplayName(recipe.resultItem)}");
            return true;
        }
        finally
        {
            if (!committed && result != null) result.ReturnToPool();
            for (int i = 0; i < lives.Count; i++)
                if (ingredients[i] != null && ingredients[i].LifeId == lives[i]) ingredients[i].Unreserve();
        }
    }
}
