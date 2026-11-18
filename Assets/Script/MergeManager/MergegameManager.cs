using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
[DefaultExecutionOrder(-200)]
public partial class MergegameManager : MonoBehaviour
{
    public ItemSeries itemSeries;
    public MergeRecipeBook recipeBook;
    public MergeDiscoveryHistory discoveryHistory;
    public List<MergeItem> mergeItems = new List<MergeItem>();
    public List<MergeItemMaterial> mergeMaterials = new List<MergeItemMaterial>();
    public MergeItemMaterial currentMaterial;

    [Header("기존 씬/풀 연결")]
    public ObjectPoolManager objectPool;
    public Transform spawnPoint;
    public MergeDropZone dropZone;

    [Header("생성 옵션")]
    [Min(0f)] public float dropInterval = 0.5f;
    [Tooltip("자동·수동 투입에 공통 적용되는 최소 간격입니다. 0이면 별도 시간 하한이 없습니다.")]
    [Min(0f)] public float minimumDropInterval = .1f;
    [Min(1)] public int maximumActiveItems = 50;
    [Min(0)] public int prewarmCount = 4;
    [Min(0f)] public float resultMergeDelay = 0.1f;
    [Min(0f)] public float dropOffset = 0.02f;

    [Header("자동 투입")]
    public bool automaticDrop;
    public bool isFeeding = true;
    [Tooltip("자동 투입이 성공하면 다음 종류를 선택합니다. 소진된 종류는 건너뜁니다.")]
    public bool cycleAutomaticMaterials = true;


    public bool logActions = true;

    [SerializeField] private string lastMessage;
    private readonly List<MergeItemObject> active = new List<MergeItemObject>();
    [Header("보너스 아이템 생성")]
    public MergeBonusSpawnSettings bonusSpawn = new MergeBonusSpawnSettings();
    private MergeItemDropper dropper;
    private MergeItemCombiner combiner;
    private MergeItemDropper Dropper => dropper ?? (dropper = new MergeItemDropper(this));
    private MergeItemCombiner Combiner => combiner ?? (combiner = new MergeItemCombiner(this));
    private bool displayMaterialUI = true;
    private bool clearing;
    private uint serial;
    private int revision;
    public IReadOnlyList<MergeItemObject> ActiveItems => active;
    public string LastMessage => lastMessage;
    public ItemData NextDropItem => Dropper.NextItem;
    public float DropCountdown => Dropper.Countdown;
    public bool IsDropTimerRunning => Dropper.IsTimerRunning;
    public float DropInterval => Dropper.Interval;
    public bool MaterialUIEnabled => displayMaterialUI;
    public virtual bool RetainSelectedItems => false;
    public bool SelectionSuppressed { get; set; }
    public virtual bool CapturesSelectionPointer(Vector2 worldPosition) => false;
    public virtual bool ForceAutomaticFeeding => false;
    public bool UsesAutomaticFeeding => automaticDrop || ForceAutomaticFeeding;
    public bool IsFeedingEnabled => isFeeding || ForceAutomaticFeeding;
    public virtual float FeedingSpeedMultiplier => 1f;
    public bool CanOperate => isActiveAndEnabled && !clearing && Time.timeScale > 0f;
    internal int Revision => revision;
    internal int ActiveItemCount
    {
        get { active.RemoveAll(piece => piece == null || !piece.IsLive); return active.Count; }
    }

    protected virtual void OnEnable() => Dropper.Begin();
    protected virtual void Start() { PrewarmItems(); RefreshUI(); }
    protected virtual void Update() => Dropper.Tick();
    protected virtual void LateUpdate() { if (CanOperate) dropZone?.TickDeadline(this); Combiner.ProcessContacts(); }
    protected virtual void OnDisable() { dropper?.End(); ClearItems(); }

    public void RefreshFeedingTiming() => Dropper.ResetCooldown();
    public void SetFeeding(bool value) => Dropper.SetFeeding(value);
    public void SetAutomaticDrop(bool value) => Dropper.SetAutomaticDrop(value);
    public bool TryDropCurrent() => Dropper.TryDropCurrent();
    public void GetUpcomingDropItems(List<ItemData> result, int maximum) => Dropper.GetUpcomingItems(result, maximum);
    public void SetAllMaterialsSelected(bool selected) => Dropper.SetAllMaterialsSelected(selected);
    public void SetMaterialStock(IReadOnlyList<InventoryItem> items) => Dropper.SetMaterialStock(items);
    public void SetConfiguredMaterialCounts(IReadOnlyList<int> counts) => Dropper.SetConfiguredMaterialCounts(counts);
    public void QueueContact(MergeItemObject a, MergeItemObject b) => Combiner.QueueContact(a, b);
    public void ProcessContacts() => Combiner.ProcessContacts();
    protected bool TryMergeWithIngredient(MergeItemObject food, MergeItem burningItem) => Combiner.TryBake(food, burningItem);

    public void RefreshUI()
    {
        if (displayMaterialUI && mergeMaterials != null)
            foreach (var material in mergeMaterials)
            {
                material?.SetCountText();
                material?.SetActive();
            }
        Dropper.RefreshMaterialSelection();
    }

    public void PrewarmItems()
    {
        if (objectPool == null || prewarmCount <= 0) return;
        var prefabs = new HashSet<GameObject>();
        if (mergeMaterials != null) foreach (var material in mergeMaterials)
        {
            if (TryGetPrefab(material?.mergeItem, out var prefab)) prefabs.Add(prefab);
        }
        foreach (var prefab in prefabs) objectPool.Prewarm(prefab, prewarmCount);
    }

    internal static bool TryGetPrefab(MergeItem definition, out GameObject prefab)
    {
        prefab = definition != null ? definition.prefab : null;
        if (prefab == null || definition.item == null) return false;
        var component = prefab.GetComponent<MergeItemObject>();
        return component != null && component.enabled;
    }

    public void SetMaterialUIEnabled(bool value)
    {
        displayMaterialUI = value;
        if (value) RefreshUI();
    }

    public MergeItemMaterial GetDefinition(ItemData item)
    {
        if (item == null) return null;
        if (mergeMaterials != null) foreach (var definition in mergeMaterials)
            if (definition?.mergeItem != null && definition.mergeItem.item == item)
                    return new MergeItemMaterial(
                        definition.mergeItem,
                        definition.mergeUI,
                        0);
        return null;
    }

    public MergeItem GetDefinitionAtMergeItem(ItemData item)
    {
        if (item == null) return null;
        var fromBook = recipeBook != null ? recipeBook.GetDefinition(item) : null;
        if (fromBook != null) return fromBook;
        if (mergeItems != null) foreach (var definition in mergeItems)
            if (definition != null && definition.item == item) return definition;
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
        Combiner.ClearContacts();
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
            revision++; Combiner.ClearPending();
            foreach (var piece in active.ToArray()) if (piece != null) piece.ReturnToPool();
            active.Clear(); dropper?.ResetCooldown();
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
