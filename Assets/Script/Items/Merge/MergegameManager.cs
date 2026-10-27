using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
[DefaultExecutionOrder(-200)]
public class MergegameManager : MonoBehaviour
{
    public ItemSeries itemSeries;
    public MergeRecipeBook recipeBook;
    [SerializeField, HideInInspector, FormerlySerializedAs("mergeRecipe")]
    private List<MergeSeries> legacyMergeRecipes = new List<MergeSeries>();
#if UNITY_EDITOR
    public IReadOnlyList<MergeSeries> LegacyRecipes => legacyMergeRecipes;
    public void ClearLegacyRecipes() => legacyMergeRecipes.Clear();
#endif
    public List<MergeItem> mergeItems = new List<MergeItem>();
    public List<MergeItemMaterial> mergeMaterials = new List<MergeItemMaterial>();
    public MergeItemMaterial currentMaterial;

    [Header("기존 씬/풀 연결")]
    public ObjectPoolManager objectPool;
    public Transform spawnPoint;

    [Header("생성 옵션")]
    [Min(0f)] public float dropInterval = 0.5f;
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
    public bool CanOperate => isActiveAndEnabled && !clearing && Time.timeScale > 0f;
    internal int Revision => revision;
    internal int ActiveItemCount
    {
        get { active.RemoveAll(piece => piece == null || !piece.IsLive); return active.Count; }
    }

    private void OnEnable() => Dropper.Begin();
    private void Start() { PrewarmItems(); RefreshUI(); }
    private void Update() => Dropper.Tick();
    private void LateUpdate() => Combiner.ProcessContacts();
    private void OnDisable() => ClearItems();

    public void SetFeeding(bool value) => Dropper.SetFeeding(value);
    public void SetAutomaticDrop(bool value) => Dropper.SetAutomaticDrop(value);
    public bool TryDropCurrent() => Dropper.TryDropCurrent();
    public void SetSessionMaterials(IReadOnlyList<InventoryItem> items, GameSession source = null) => Dropper.SetSessionMaterials(items, source);
    public void ResetRoundMaterials() => Dropper.ResetRoundMaterials();
    public void RefreshSessionMaterials(IReadOnlyList<InventoryItem> items) => Dropper.RefreshSessionMaterials(items);
    public void QueueContact(MergeItemObject a, MergeItemObject b) => Combiner.QueueContact(a, b);
    public void ProcessContacts() => Combiner.ProcessContacts();

    public void RefreshUI()
    {
        if (!displayMaterialUI || mergeMaterials == null) return;
        Debug.Log("re");
        foreach(var material in mergeMaterials)
        {
            material?.SetCountText();
            material?.SetActive();
        }
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
        if (prefab == null) return false;
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
                        0, definition.defaultCount);
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
        if (!TryGetPrefab(definition, out var prefab)) return null;
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
        Combiner.ClearPending();
        foreach (var piece in active)
        {
            if (piece == null || !piece.IsLive) continue;
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
        lastMessage = message;
        if (logActions) Debug.Log("[MergeGame] " + message, this);
    }
}
