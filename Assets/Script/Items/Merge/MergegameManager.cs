using System;
using System.Collections.Generic;
using UnityEngine;
[DefaultExecutionOrder(-200)]
public class MergegameManager : MonoBehaviour
{
    public ItemSeries itemSeries;
    public List<MergeSeries> mergeRecipe = new List<MergeSeries>();
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
    private readonly List<Contact> contacts = new List<Contact>();
    private readonly HashSet<ulong> queued = new HashSet<ulong>();
    private readonly Collider2D[] overlap = new Collider2D[32];
    private float nextDropAt;
    private float nextAutomaticDropAt;
    private bool displayMaterialUI = true;
    private uint serial;
    private int revision;
    private bool dropping, processing, clearing;
    private GameSession materialSession;
    private bool usesSessionMaterials;
    private int bonusRevision;
    private readonly Dictionary<MergeItemMaterial, int> roundBonuses = new Dictionary<MergeItemMaterial, int>();
    public IReadOnlyList<MergeItemObject> ActiveItems => active;
    public string LastMessage => lastMessage;
    public bool CanOperate => isActiveAndEnabled && !clearing && Time.timeScale > 0f;
    private struct Contact { public MergeItemObject a, b; public int aLife, bLife; }

    private void OnEnable()
    {
        nextAutomaticDropAt = Time.time + AutomaticInterval;
        EnsureSelection();
    }
    private void Start()
    {
        PrewarmItems();
        if(GameSession.Instance == null)
            RefreshUI();
    }
    public void RefreshUI()
    {
        if (!displayMaterialUI || mergeMaterials == null) return;
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
    private static bool TryGetPrefab(MergeItem definition, out GameObject prefab)
    {
        prefab = definition != null ? definition.prefab : null;
        if (prefab == null) return false;
        var component = prefab.GetComponent<MergeItemObject>();
        return component != null && component.enabled;
    }
    private float AutomaticInterval => Mathf.Max(.01f, EffectStatUtility.Safe(dropInterval, 0f, 600f, .5f));
    private void Update()
    {
        if (!automaticDrop || !isFeeding || !CanOperate ||
            (GameInputManager.instance != null && GameInputManager.instance.IsBlocked(GameInputBlockType.Gameplay)))
        {
            nextAutomaticDropAt = Time.time + AutomaticInterval;
            return;
        }
        if (Time.time < nextAutomaticDropAt) return;
        // 막힌 투입구도 매 프레임 재시도하지 않고 다음 주기까지 기다립니다.
        nextAutomaticDropAt = Time.time + AutomaticInterval;
        EnsureSelection();
        var usedMaterial = currentMaterial;
        if (TryDropCurrent() && cycleAutomaticMaterials)
            currentMaterial = NextFeedingMaterial(usedMaterial);
    }
    public void SetFeeding(bool value)
    {
        isFeeding = value;
        nextAutomaticDropAt = Time.time + AutomaticInterval;
    }
    public void SetAutomaticDrop(bool value)
    {
        automaticDrop = value;
        nextAutomaticDropAt = Time.time + AutomaticInterval;
    }
    public void SetMaterialUIEnabled(bool value)
    {
        displayMaterialUI = value;
        if (value) RefreshUI();
    }
    private void OnDisable() => ClearItems();

    // 재고와 토글을 함께 검사합니다. 토글이 꺼졌거나 연결되지 않은 재료는 투입하지 않습니다.
    private static bool CanFeedMaterial(MergeItemMaterial material)
        => MergeTree.IsAvailable(material) && material.mergeUI != null &&
            material.mergeUI.toggle != null && material.mergeUI.toggle.isOn;

    private MergeItemMaterial NextFeedingMaterial(MergeItemMaterial current)
    {
        if (mergeMaterials == null || mergeMaterials.Count == 0) return null;
        int start = mergeMaterials.IndexOf(current);
        for (int offset = 1; offset <= mergeMaterials.Count; offset++)
        {
            var candidate = mergeMaterials[(start + offset) % mergeMaterials.Count];
            if (CanFeedMaterial(candidate)) return candidate;
        }
        return null;
    }

    private void EnsureSelection()
    {
        if (mergeMaterials != null && currentMaterial != null)
        {
            if (mergeMaterials.Contains(currentMaterial) && CanFeedMaterial(currentMaterial)) return;
            foreach (var material in mergeMaterials)
                if (CanFeedMaterial(material) && material.mergeItem == currentMaterial.mergeItem) { currentMaterial = material; return; }
        }
        currentMaterial = NextFeedingMaterial(currentMaterial);
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
        if (mergeItems != null) foreach (var definition in mergeItems)
                if (definition != null && definition.item == item)
                    return definition;
        return null;

    }
    // 시작 재고를 복사합니다. 전달 데이터와 현재 재료의 수량 객체를 공유하지 않습니다.
    public void SetSessionMaterials(IReadOnlyList<InventoryItem> items, GameSession source = null)
    {
        materialSession = source;
        usesSessionMaterials = source != null;
        ResetRoundMaterials();
        RefreshSessionMaterials(items);
    }

    public void ResetRoundMaterials()
    {
        bonusRevision++;
        ClearItems();
        if (mergeMaterials == null) return;
        foreach (var material in mergeMaterials)
        {
            if (material == null) continue;
            // 기본 수량의 설정값을 따로 기억해 재시도 때만 보너스를 다시 지급합니다.
            if (!roundBonuses.TryGetValue(material, out int bonus))
                roundBonuses.Add(material, bonus = Math.Max(0, material.defaultCount));
            material.defaultCount = bonus;
        }
        EnsureSelection();
        RefreshUI();
    }

    public void RefreshSessionMaterials(IReadOnlyList<InventoryItem> items)
    {
        if (mergeMaterials == null) return;
        foreach (var material in mergeMaterials)
        {
            if (material?.mergeItem == null) continue;
            long count = 0;
            if (items != null && material.mergeItem.item != null && material.mergeItem.item.series == itemSeries)
                foreach (var item in items)
                    if (item?.itemData == material.mergeItem.item) count += Math.Max(0, item.amount);
            material.count = (int)Math.Min(int.MaxValue, count);
        }
        EnsureSelection();
        RefreshUI();
    }
    public bool TryDropCurrent()
    {
        if (!isFeeding) return Reject("투입이 중지되어 있습니다.");
        if (!CanOperate || dropping) return Reject("현재 생성할 수 없는 상태입니다.");
        if (objectPool == null || spawnPoint == null) return Reject("Object Pool / Spawn Point 연결을 확인하세요.");
        if (Time.time < nextDropAt) return Reject("다음 생성까지 대기 중입니다.");
        active.RemoveAll(piece => piece == null || !piece.IsLive);
        if (active.Count >= Mathf.Max(1, maximumActiveItems)) return Reject("최대 활성 아이템 개수에 도달했습니다.");
        EnsureSelection();
        if (!CanFeedMaterial(currentMaterial)) return Reject("토글이 켜진 사용 가능한 재료가 없습니다.");
        var material = currentMaterial;
        if (!TryGetPrefab(material.mergeItem, out _))
            return Reject("선택 재료의 MergeItem → Prefab에 활성 MergeItemObject가 있는 프리팹을 연결하세요.");
        float offset = UnityEngine.Random.Range(-dropOffset, dropOffset);
        Vector3 position = spawnPoint.position + new Vector3(offset * dropOffset, 0f, 0f);
        if (IsBlocked(position, material.mergeItem.Radius)) return Reject("투입 위치가 막혀 있습니다.");
        dropping = true;
        int before = revision;
        bool committed = false;
        bool reserved = false;
        bool usedBonus = material.defaultCount > 0;
        bool useSessionStock = usesSessionMaterials;
        int reservedBonusRevision = bonusRevision;
        var source = materialSession;
        int sourceRevision = source != null ? source.GameDataRevision : -1;
        MergeItemObject piece = null;
        try
        {
            // 풀 콜백 전에 예약해 마지막 재료가 중복 투입되는 것을 막습니다.
            if (usedBonus) material.defaultCount--;
            else if (useSessionStock)
            {
                if (source == null || !source.TryConsumeMaterial(material.mergeItem.item))
                    return Reject("남은 본재고가 없습니다.");
                material.count = source.GetMaterialCount(material.mergeItem.item);
            }
            else material.count--;
            reserved = true;
            if (before != revision || !CanOperate) return false;
            piece = SpawnPiece(material.mergeItem, position);
            if (piece == null || !piece.IsLive || before != revision || !CanOperate)
                return Reject("풀 생성이 취소되었거나 아이템이 비활성 상태입니다.");
            piece.SetSimulated(true);
            nextDropAt = Time.time + EffectStatUtility.Safe(dropInterval, 0f, 600f, 0.5f);
            committed = true;
            RefreshUI();
            EnsureSelection();
            Report($"낙하: {DisplayName(material.mergeItem.item)}, 해당 재료 잔량: {material.count}");
            return true;
        }
        finally
        {
            if (!committed)
            {
                if (piece != null) piece.ReturnToPool();
                if (reserved)
                {
                    if (usedBonus)
                    {
                        if (reservedBonusRevision == bonusRevision) material.defaultCount++;
                    }
                    else if (useSessionStock)
                    {
                        source?.RefundMaterial(material.mergeItem.item, sourceRevision);
                        if (source != null && source.GameDataRevision == sourceRevision)
                            material.count = source.GetMaterialCount(material.mergeItem.item);
                    }
                    else material.count++;
                    RefreshUI();
                }
            }
            dropping = false;
        }
    }
    private bool IsBlocked(Vector2 position, float radius)
    {
        foreach (var piece in active)
            if (piece != null && piece.IsLive && (piece.Position - position).sqrMagnitude < Mathf.Pow(piece.Definition.Radius + radius, 2f)) return true;
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = false };
        var physics = gameObject.scene.GetPhysicsScene2D();
        return !physics.IsValid() || physics.OverlapCircle(position, radius, filter, overlap) > 0;
    }
    private MergeItemObject SpawnPiece(MergeItem definition, Vector3 position)
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
    // 화면 크기 변경으로 벽/투입구가 이동할 때, 풀의 활성 아이템도 같은 좌표 변환을 적용합니다.
    // 아이템 데이터·반지름·개수는 유지하고 원형 콜라이더를 비균등하게 늘리지 않습니다.
    public void RemapLayout(Matrix4x4 change)
    {
        contacts.Clear(); queued.Clear();
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
    public void QueueContact(MergeItemObject a, MergeItemObject b)
    {
        if (!CanOperate || a == null || b == null || a == b || a.Owner != this || b.Owner != this || !a.CanMerge || !b.CanMerge) return;
        ulong key = a.Serial < b.Serial ? ((ulong)a.Serial << 32) | b.Serial : ((ulong)b.Serial << 32) | a.Serial;
        if (queued.Add(key)) contacts.Add(new Contact { a = a, b = b, aLife = a.LifeId, bLife = b.LifeId });
    }
    private void LateUpdate() => ProcessContacts();
    public void ProcessContacts()
    {
        if (processing) return;
        var pending = contacts.ToArray(); contacts.Clear(); queued.Clear(); processing = true;
        try
        {
            foreach (var contact in pending)
            {
                var a = contact.a; var b = contact.b;
                if (!CanOperate) break;
                if (a == null || b == null || a.LifeId != contact.aLife || b.LifeId != contact.bLife ||
                    a.Owner != this || b.Owner != this || !a.CanMerge || !b.CanMerge) continue;
                TryMerge(a, b);
            }
        }
        finally { processing = false; }
    }
    private void TryMerge(MergeItemObject a, MergeItemObject b)
    {
        if (!MergeTree.TryGetRecipe(mergeRecipe, a.item, b.item, out var recipe)) return;
        var definition = GetDefinitionAtMergeItem(recipe.resultItem);
        if (objectPool == null || !TryGetPrefab(definition, out _)) return;
        if (!a.Reserve()) return;
        if (!b.Reserve()) { a.Unreserve(); return; }
        int before = revision, aLife = a.LifeId, bLife = b.LifeId;
        MergeItemObject result = null;
        bool committed = false;
        try
        {
            Vector2 velocity = (a.Velocity * a.body.mass + b.Velocity * b.body.mass) / Mathf.Max(.001f, a.body.mass + b.body.mass);
            result = SpawnPiece(definition, (a.transform.position + b.transform.position) * 0.5f);
            if (result == null || !result.IsLive || before != revision || !CanOperate || a.LifeId != aLife || b.LifeId != bLife || !a.IsLive || !b.IsLive) return;
            a.ReturnToPool(); b.ReturnToPool();
            result.Velocity = velocity; result.SetSimulated(true); committed = true;

            Report($"합성: {DisplayName(recipe.resultItem)}");
        }
        finally
        {
            if (!committed && result != null) result.ReturnToPool();
            if (a != null && a.LifeId == aLife) a.Unreserve();
            if (b != null && b.LifeId == bLife) b.Unreserve();
        }
    }
    public void ClearItems()
    {
        if (clearing) return;
        clearing = true;
        try
        {
            revision++; contacts.Clear(); queued.Clear();
            foreach (var piece in active.ToArray()) if (piece != null) piece.ReturnToPool();
            active.Clear(); nextDropAt = 0f;
        }
        finally { clearing = false; }
    }
    private bool Reject(string message) { Report(message); return false; }
    private static string DisplayName(ItemData item)
    {
        if (item == null) return "";
        // 아이템씬 단독 실행에서는 코어의 언어 매니저가 없어도 조작/로그가 동작합니다.
        string localized = LanguageManager.instance != null ? item.GetDataName() : null;
        return !string.IsNullOrEmpty(localized) ? localized : item.name;
    }
    private void Report(string message)
    {
        lastMessage = message;
        if (logActions) Debug.Log("[MergeGame] " + message, this);
    }
}
