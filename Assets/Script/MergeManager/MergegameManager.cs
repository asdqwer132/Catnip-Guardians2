using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-200)]
public partial class MergegameManager : MonoBehaviour
{
    [SerializeField] private MergeRecipeBook[] recipeBooks = System.Array.Empty<MergeRecipeBook>();
    public IReadOnlyList<MergeRecipeBook> RecipeBooks => recipeBooks;
    public MergeDiscoveryHistory discoveryHistory;
    public ObjectPoolManager objectPool;
    [Min(1)] public int maximumActiveItems = 50;
    [Min(0)] public int prewarmCount = 4;
    [Min(0f)] public float resultMergeDelay = .1f;

    public bool logActions = true;

    [SerializeField] private string lastMessage;
    private readonly List<MergeItemObject> active = new List<MergeItemObject>();
    [Header("보너스 아이템 생성")]
    public MergeBonusSpawnSettings bonusSpawn = new MergeBonusSpawnSettings();
    private MergeItemCombiner combiner;
    private MergeItemCombiner Combiner => combiner ??= new MergeItemCombiner(this);
    private bool clearing;
    private uint serial;
    private int revision;
    public IReadOnlyList<MergeItemObject> ActiveItems => active;
    public string LastMessage => lastMessage;
    public bool CanOperate => isActiveAndEnabled && !clearing && Time.timeScale > 0f;
    internal int Revision => revision;
    internal int ActiveItemCount
    {
        get { active.RemoveAll(piece => piece == null || !piece.IsLive); return active.Count; }
    }

    private void OnDisable()
    {
        GameSession.InstanceChanged -= BindSession;
        UnbindSession();
        gimmickManager?.StopGimic();
        CancelClaw();
        ReleaseRoots();
        ClearMagic();
        ClearMonsters();
        ClearItems();
    }

    internal bool TryMergeAtPin(MergeItemObject piece, Vector2 center, float radius)
        => Combiner.TryMergeAtPin(piece, center, radius);
    protected bool TryMergeWithIngredient(MergeItemObject food, MergeItem burningItem) => Combiner.TryBake(food, burningItem);

    internal static bool TryGetPrefab(MergeItem definition, out GameObject prefab)
    {
        prefab = definition != null ? definition.prefab : null;
        if (prefab == null || definition.item == null) return false;
        var component = prefab.GetComponent<MergeItemObject>();
        return component != null && component.enabled;
    }

    internal IEnumerable<ItemRecipeData> GetRoundRecipes()
    {
        foreach (var book in recipeBooks)
            if (book != null)
                foreach (var recipe in book.Recipes) yield return recipe;
    }

    public MergeItem GetDefinitionAtMergeItem(ItemData item)
    {
        if (item == null) return null;
        foreach (var book in recipeBooks)
        {
            var definition = book != null ? book.GetDefinition(item) : null;
            if (definition != null) return definition;
        }
        return null;
    }

    internal MergeItemObject SpawnPiece(MergeItem definition, Vector3 position)
    {
        if (objectPool == null || !TryGetPrefab(definition, out var prefab)) return null;
        var root = objectPool.Spawn(prefab, position, Quaternion.identity);
        if (root == null) return null;
        var piece = root.GetComponent<MergeItemObject>();
        if (piece == null) { objectPool.Release(root); return null; }
        int spawnId = piece.PoolSpawnId;
        try
        {
            // General이 아이템 씬에 할당한 레이어를 새 객체/Visual에도 이어 줍니다.
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = gameObject.layer;
            unchecked { serial++; if (serial == 0) serial++; }
            piece.Initialize(this, definition, serial, resultMergeDelay);
            active.Add(piece);
            return piece;
        }
        catch
        {
            objectPool.Release(root, spawnId);
            throw;
        }
    }

    internal void NotifyReturned(MergeItemObject piece) => active.Remove(piece);

    public void RemapLayout(Matrix4x4 change)
    {
        foreach (var piece in active)
        {
            if (piece == null || !piece.IsLive) continue;
            piece.RemapPendingSpawns(change);
            Vector3 position = change.MultiplyPoint3x4(piece.transform.position);
            piece.body.position = position;
            piece.transform.position = position;
            piece.Velocity = change.MultiplyVector(piece.Velocity);
        }
    }

    public string GetItemDisplayName(ItemData item) => DisplayName(item);

    public bool TryRemoveItem(MergeItemObject piece, int expectedLifeId)
    {
        if (!CanOperate || piece == null || piece.Owner != this || piece.LifeId != expectedLifeId || !piece.CanSelect) return false;
        piece.ReturnToPool();
        return true;
    }

    public void ClearItems()
    {
        if (clearing) return;
        clearing = true;
        try
        {
            revision++;
            foreach (var piece in active.ToArray()) if (piece != null) piece.ReturnToPool();
            active.Clear();
        }
        finally { clearing = false; }
    }

    internal bool Reject(string message) { Report(message); return false; }

    private static string DisplayName(ItemData item)
    {
        if (item == null) return "";
        // 아이템씬 단독 실행에서는 코어의 언어 매니저가 없어도 조작/로그가 동작합니다.
        string localized = LanguageManager.instance != null ? item.GetDataName() : null;
        return !string.IsNullOrEmpty(localized) ? localized : item.name;
    }

    internal void Report(string message)
    {
        if (lastMessage == message) return;
        lastMessage = message;
        if (logActions) Debug.Log("[MergeGame] " + message, this);
    }
}
