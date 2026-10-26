using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
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

    [Header("테스트 입력 · Input System")]
    public bool acceptKeyboardInput = true;
    public string dropBinding = "<Keyboard>/space";
    public string nextMaterialBinding = "<Keyboard>/q";
    public bool logActions = true;

    [Header("직접 만든 UI 연결용")]
    public UnityEvent onCurrentMaterialChanged = new UnityEvent();
    public UnityEvent onMaterialsChanged = new UnityEvent();
    public UnityEvent onBoardChanged = new UnityEvent();
    public UnityEvent onItemMerged = new UnityEvent();

    [SerializeField] private string lastMessage;
    private readonly List<MergeItemObject> active = new List<MergeItemObject>();
    private readonly List<Contact> contacts = new List<Contact>();
    private readonly HashSet<ulong> queued = new HashSet<ulong>();
    private readonly Collider2D[] overlap = new Collider2D[32];
    private InputAction dropInput, nextMaterialInput;
    private float nextDropAt;
    private uint serial;
    private int revision;
    private bool dropping, processing, clearing;
    private GameSession materialSession;
    private bool usesSessionMaterials;
    private int bonusRevision;
    private readonly Dictionary<MergeItemMaterial, int> roundBonuses = new Dictionary<MergeItemMaterial, int>();
    public IReadOnlyList<MergeItemObject> ActiveItems => active;
    public Sprite CurrentMaterialIcon => currentMaterial?.mergeItem != null && currentMaterial.mergeItem.item != null ? currentMaterial.mergeItem.item.icon : null;
    public string CurrentMaterialName => DisplayName(currentMaterial?.mergeItem != null ? currentMaterial.mergeItem.item : null);
    public int CurrentMaterialCount => currentMaterial != null ? Math.Max(0, currentMaterial.count) : 0;
    public int CurrentDefaultMaterialCount => currentMaterial != null ? Math.Max(0, currentMaterial.defaultCount) : 0;
    public string LastMessage => lastMessage;
    public bool CanOperate => isActiveAndEnabled && !clearing && Time.timeScale > 0f;
    private struct Contact { public MergeItemObject a, b; public int aLife, bLife; }

    private void OnEnable()
    {
        EnsureSelection();
        if (dropInput == null)
        {
            dropInput = new InputAction("Drop merge item", InputActionType.Button, dropBinding);
            nextMaterialInput = new InputAction("Next merge material", InputActionType.Button, nextMaterialBinding);
            dropInput.performed += _ => TryDropCurrent();
            nextMaterialInput.performed += _ => SelectNextMaterial();
        }
        ApplyInputState();
    }
    private void Start()
    {
        PrewarmItems();
        Notify(onCurrentMaterialChanged);
        if(GameSession.Instance == null)
            RefreshUI();
    }
    public void RefreshUI()
    {
        if (mergeMaterials == null) return;
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
    private void Update() => ApplyInputState();
    private void ApplyInputState()
    {
        if (dropInput == null) return;
        if (acceptKeyboardInput && isActiveAndEnabled) { dropInput.Enable(); nextMaterialInput.Enable(); }
        else { dropInput.Disable(); nextMaterialInput.Disable(); }
    }
    public void SetKeyboardInputEnabled(bool value) { acceptKeyboardInput = value; ApplyInputState(); }
    private void OnDisable()
    {
        dropInput?.Disable(); nextMaterialInput?.Disable(); ClearItems();
    }
    private void OnDestroy() { dropInput?.Dispose(); nextMaterialInput?.Dispose(); }
    private void EnsureSelection()
    {
        if (mergeMaterials != null && currentMaterial != null)
        {
            if (mergeMaterials.Contains(currentMaterial) && MergeTree.IsAvailable(currentMaterial)) return;
            foreach (var material in mergeMaterials)
                if (MergeTree.IsAvailable(material) && material.mergeItem == currentMaterial.mergeItem) { currentMaterial = material; return; }
        }
        currentMaterial = MergeTree.NextMaterial(mergeMaterials, null);
    }
    public bool SelectMaterial(int index)
    {
        if (mergeMaterials == null || index < 0 || index >= mergeMaterials.Count || !MergeTree.IsAvailable(mergeMaterials[index])) return false;
        currentMaterial = mergeMaterials[index]; SelectionChanged(); return true;
    }
    // Unity Button/Dropdown에는 반환값이 없는 메서드를 연결합니다.
    public void DropCurrent() => TryDropCurrent();
    public void NextMaterial() => SelectNextMaterial();
    public void SetMaterial(int index) => SelectMaterial(index);

    public bool SelectNextMaterial()
    {
        currentMaterial = MergeTree.NextMaterial(mergeMaterials, currentMaterial);
        SelectionChanged(); return currentMaterial != null;
    }
    private void SelectionChanged()
    {
        Notify(onCurrentMaterialChanged);


        Report($"선택 재료: {CurrentMaterialName}, 남은 개수: {CurrentMaterialCount}");
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
        Notify(onMaterialsChanged);
        Notify(onCurrentMaterialChanged);
    }
    public bool TryDropCurrent()
    {
        if (!CanOperate || dropping) return Reject("현재 생성할 수 없는 상태입니다.");
        if (objectPool == null || spawnPoint == null) return Reject("Object Pool / Spawn Point 연결을 확인하세요.");
        if (Time.time < nextDropAt) return Reject("다음 생성까지 대기 중입니다.");
        active.RemoveAll(piece => piece == null || !piece.IsLive);
        if (active.Count >= Mathf.Max(1, maximumActiveItems)) return Reject("최대 활성 아이템 개수에 도달했습니다.");
        EnsureSelection();
        if (!MergeTree.IsAvailable(currentMaterial)) return Reject("사용할 재료가 없습니다.");
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
            Notify(onBoardChanged); Notify(onMaterialsChanged);
            EnsureSelection(); Notify(onCurrentMaterialChanged);
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
        Notify(onBoardChanged);
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
            Notify(onBoardChanged); Notify(onItemMerged);
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
            active.Clear(); nextDropAt = 0f; Notify(onBoardChanged);
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
    private void Notify(UnityEvent callback)
    {
        try { callback?.Invoke(); }
        catch (Exception exception) { Debug.LogException(exception, this); }
    }
}
