using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

[Serializable]
public sealed class SummonModuleRuntimeView
{
    public SummonBehaviourModule module;
    [SerializeReference] public object currentStat;
}

public enum SummonThrowTargetMode { NearestEnemy, RandomEnemy, RandomPosition }

// 모듈의 실행 상태를 관리합니다. 기본 기능 스탯은 각 모듈 내부에 있습니다. 생성 개수/배치/수명은 SpawnEffect가 전달합니다.
[BuffTargetGroups("Summon")]
[MovedFrom(true, null, "Assembly-CSharp", "SummonItemThrower")]
public class SummonActor : AttackObjectBase, IBuffTarget, IDynamicBuffReceiver, IDamageable, IPrefabVisualLifetime, IPrefabRangeProvider
{
    [Header("소환물 종류 / 태그")]
    public SummonDefinition definition;
    [Header("Behaviour Modules")]
    [Tooltip("행동 에셋을 원하는 순서로 조합합니다. 이동 모듈을 공격 모듈보다 앞에 배치하세요.")]
    public SummonBehaviourModule[] modules;
    [Header("Item Throw Component")]
    public ItemThrowExecutor itemThrowExecutor;
    [Header("Detect")]
    public LayerMask enemyLayerMask;
    [Header("Runtime Stat")]
    [SerializeField] private List<SummonModuleRuntimeView> runtimeModuleStats = new List<SummonModuleRuntimeView>();
    [FormerlySerializedAs("lifeTime")]
    [SerializeField] private float spawnLifetime = 5f;
    private float extraLifetime;
    private ItemData sourceItemData;
    private EquipmentBag sourceBag;
    private BuffManager buffManager;
    private ItemEffectLease effectLease;
    private GameObject owner;
    private readonly List<SummonBehaviourRuntime> behaviours = new List<SummonBehaviourRuntime>();
    private readonly List<SummonModification> modifications = new List<SummonModification>();
    private readonly HashSet<SummonTransformEffect> completedTransforms = new HashSet<SummonTransformEffect>();
    private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
    private ItemEffectContext executionContext;
    private SummonBehaviourModule[] effectiveModules;
    private Health health;
    private bool hasHealthModule;
    private int healthInitializedLife = int.MinValue;
    private float timer;
    private bool initialized, registered, finishing;
    private bool groupReady, groupPendingReady, groupPendingActivation;
    public bool IsGroupReady => groupReady;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "Summon";
    public string BuffTargetDebugName => name;
    public GameObject Owner => owner;
    public SummonDefinition Definition => definition;
    public Health Health => hasHealthModule ? health : null;
    public Transform DamageTransform => transform;
    // HP 없는 일반 공격 소환물은 적의 피격 대상으로 선택하지 않는다.
    public bool IsDead => finishing || Health == null || Health.IsDead;
    public int LifeId { get; private set; }
    public int ProfileVersion { get; private set; }
    public int AttackCount { get; private set; }
    public IReadOnlyList<SummonBehaviourModule> ActiveModules => effectiveModules ?? new SummonBehaviourModule[0];
    public float RemainingLifeTime => Mathf.Max(0f, spawnLifetime + extraLifetime - timer);
    public Transform ControlledRangeVisual => null;
    public bool TryGetVisualLifetime(out float remaining)
    { remaining = RemainingLifeTime; return initialized && !finishing; }
    public void CollectPrefabRanges(List<PrefabVisualRange> results)
    {
        if (initialized)
        {
            foreach (SummonModuleRuntimeView view in runtimeModuleStats)
                CollectModuleRange(view.module, view.currentStat, results);
            return;
        }
        // 프리팹 편집 중에는 실제 실행할 모듈의 기본값을 복제·정규화해서 표시합니다.
        foreach (SummonBehaviourModule module in effectiveModules ?? modules ?? new SummonBehaviourModule[0])
        {
            object current = null;
            if (module is SummonContactDamageModule contact) { var value = contact.baseStat?.Clone() ?? new SummonContactDamageStat(); value.Clamp(); current = value; }
            else if (module is SummonContactEffectModule effect) { var value = effect.baseStat?.Clone() ?? new SummonContactEffectStat(); value.Clamp(); current = value; }
            else if (module is SummonTurretModule turret) { var value = turret.baseStat?.Clone() ?? new SummonTurretStat(); value.Clamp(); current = value; }
            else if (module is SummonItemThrowModule throwing) { var value = throwing.baseStat?.Clone() ?? new SummonItemThrowStat(); value.Clamp(); current = value; }
            else if (module is SummonPullModule pull) { var value = pull.baseStat?.Clone() ?? new SummonPullStat(); value.Clamp(); current = value; }
            else if (module is SummonAuraModule aura) { var value = aura.baseStat?.Clone() ?? new SummonAuraStat(); value.Clamp(); current = value; }
            CollectModuleRange(module, current, results);
        }
    }
    private void CollectModuleRange(SummonBehaviourModule module, object current, List<PrefabVisualRange> results)
    {
        float radius; PrefabRangeKind kind = PrefabRangeKind.Detection;
        if (current is SummonContactDamageStat contact) { radius = contact.radius; kind = PrefabRangeKind.Damage; }
        else if (current is SummonContactEffectStat effect) { radius = effect.radius; kind = PrefabRangeKind.Damage; }
        else if (current is SummonTurretStat turret) radius = turret.range;
        else if (current is SummonItemThrowStat throwing) radius = throwing.range;
        else if (current is SummonPullStat pull) radius = pull.range;
        else if (current is SummonAuraStat aura) { radius = aura.range; kind = PrefabRangeKind.Support; }
        else return;
        results.Add(PrefabVisualRange.Circle(transform.position, radius, module != null ? module.name : current.GetType().Name, kind));
    }
    public bool IsTransformReserved { get; internal set; }
    public bool CanAct => initialized && !finishing && isActiveAndEnabled &&
        (Health == null || !Health.IsDead) && (executionContext == null || executionContext.CanContinue);

    protected virtual void Awake()
    {
        if (itemThrowExecutor == null) itemThrowExecutor = GetComponent<ItemThrowExecutor>();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        unchecked { LifeId++; }
        SummonRegistry.Register(this);
        timer = 0f;
        extraLifetime = 0f;
        AttackCount = 0;
        completedTransforms.Clear();
        IsTransformReserved = false;
        finishing = false;
        if (initialized)
        {
            RegisterDynamicBuffReceiver();
            RefreshBuffedStat();
            RefreshProfiles(true);
            if (groupPendingReady) NotifyGroupReady();
            else groupPendingActivation = true;
        }
        RegisterBuffTarget();
    }

    private void Start()
    {
        // 씬에 직접 배치한 기존 소환물도 자신의 기능 설정으로 시작합니다.
        if (initialized) return;
        Initialize(new ItemEffectContext(null, null, transform.position, transform.position, null,
            buffManager: BuffManager.instance), spawnLifetime);
    }

    public void ConfigureModules(SummonBehaviourModule[] overrides)
    {
        modules = overrides != null ? (SummonBehaviourModule[])overrides.Clone() : new SummonBehaviourModule[0];
        if (initialized) RefreshProfiles(true);
    }

    public void ConfigureDefinition(SummonDefinition value)
    { definition = value; SummonGroupRuntime.NotifyFieldChanged(); }

    public bool TryGetSpawnDirection(out Vector3 direction)
    {
        direction = Vector3.right;
        return executionContext != null && executionContext.TryGetDirection(out direction);
    }

    private void LateUpdate()
    {
        // 재활성화 직후 같은 프레임의 Initialize가 새 소유자/정의를 설정할 시간을 줍니다.
        if (groupPendingActivation && initialized && isActiveAndEnabled) NotifyGroupReady();
    }
    private void NotifyGroupReady()
    {
        groupPendingReady = groupPendingActivation = false;
        groupReady = CanAct;
        if (groupReady) SummonGroupRuntime.NotifyReady(this);
    }

    public ItemEffectContext CreateContext(Vector3 target, Vector3 direction)
    {
        if (executionContext != null)
        {
            ItemEffectContext result = executionContext.Copy(target, direction);
            result.usePosition = transform.position;
            result.sourceSummon = this;
            result.sourceSummonLifeId = LifeId;
            return result;
        }
        return new ItemEffectContext(owner, sourceItemData, transform.position, target, sourceBag,
            buffManager: buffManager, direction: direction) { sourceSummon = this, sourceSummonLifeId = LifeId };
    }

    public ItemEffectContext CreateAttackContext(Vector3 target, Vector3 direction, float damageMultiplier = 1f, float healingMultiplier = 1f)
    {
        ItemEffectContext context = CreateContext(target, direction);
        context.damageMultiplier *= CurrentDamageMultiplier * damageMultiplier;
        context.healingMultiplier *= CurrentHealingMultiplier * healingMultiplier;
        return context;
    }

    public bool TryTarget(SummonThrowTargetMode mode, float range, out Vector3 target)
    {
        target = transform.position;
        if (mode == SummonThrowTargetMode.RandomPosition)
        { target += (Vector3)(UnityEngine.Random.insideUnitCircle * range); return true; }
        query.Scan(transform.position, range, enemyLayerMask);
        Enemy enemy = EnemyQueryBuffer.Select(query.Enemies, transform.position, mode == SummonThrowTargetMode.RandomEnemy);
        if (enemy == null) return false;
        target = enemy.transform.position;
        return true;
    }

    public bool ThrowItem(ItemData item, Vector3 target, float? damageOverride = null, float damageMultiplier = 1f, float healingMultiplier = 1f)
    {
        if (!CanAct || item == null) return false;
        GetOrCreateItemThrowExecutor();
        ItemEffectContext context = CreateAttackContext(target, (target - transform.position).normalized, damageMultiplier, healingMultiplier);
        if (damageOverride.HasValue) context.damageOverride = damageOverride;
        if ((target - transform.position).sqrMagnitude <= 0.0001f)
        {
            ItemEffectExecutor.ExecuteItem(item, transform.position, target, context.direction,
                owner, sourceBag, buffManager, context, false, isThrownItem: true);
            NotifyAttack();
            return true;
        }
        bool thrown = itemThrowExecutor.Throw(item, transform.position, target, owner, sourceBag, 0, context, false);
        if (thrown) NotifyAttack();
        return thrown;
    }

    public ItemThrowExecutor GetOrCreateItemThrowExecutor()
    {
        if (itemThrowExecutor != null) return itemThrowExecutor;
        ItemEffectExecutor executor = GetComponent<ItemEffectExecutor>();
        if (executor == null) { executor = gameObject.AddComponent<ItemEffectExecutor>(); executor.buffManager = buffManager; }
        itemThrowExecutor = GetComponent<ItemThrowExecutor>();
        if (itemThrowExecutor == null) itemThrowExecutor = gameObject.AddComponent<ItemThrowExecutor>();
        if (itemThrowExecutor.itemEffectExecutor == null) itemThrowExecutor.itemEffectExecutor = executor;
        itemThrowExecutor.showTargetRange = false;
        return itemThrowExecutor;
    }

    public void Initialize(ItemEffectContext context, float lifetime)
    {
        if (context == null || !context.CanContinue) return;
        groupReady = groupPendingReady = groupPendingActivation = false;
        SummonGroupRuntime.NotifyFieldChanged();
        UnregisterBuffTarget();
        UnregisterDynamicBuffReceiverInternal();
        if (effectLease != null) effectLease.Cancel();
        effectLease = null;
        initialized = false;
        ClearModifications();
        DisposeBehaviours();
        unchecked { LifeId++; }
        completedTransforms.Clear();
        IsTransformReserved = false;
        AttackCount = 0;
        extraLifetime = 0f;
        timer = 0f;
        finishing = false;
        sourceItemData = context.sourceItemData;
        sourceBag = context.sourceBag;
        buffManager = context.buffManager;
        owner = context.owner;
        spawnLifetime = EffectStatUtility.Safe(lifetime, 0.01f, 600f, 5f);
        executionContext = context.Copy(transform.position, context.direction);
        effectLease = context.RetainLifetime();
        initialized = true;
        RefreshBuffedStat();
        RegisterDynamicBuffReceiver();
        RegisterBuffTarget();
        RefreshProfiles(true);
        groupPendingReady = true;
        if (isActiveAndEnabled) NotifyGroupReady();
    }

    public Health ConfigureHealth(SummonHealthStat stat, bool initialize, HealthDefenseFormula defenseFormula)
    {
        if (health == null) health = GetComponent<Health>();
        if (health == null) health = gameObject.AddComponent<Health>();
        hasHealthModule = true;
        health.enabled = true;
        health.OnDead -= OnSummonDead;
        health.OnDead += OnSummonDead;
        health.team = HealthTeam.Ally;
        health.buffTargetGroup = "SummonHealth";
        health.defenseFormula = defenseFormula;
        health.SetBuffManager(buffManager);
        bool fill = initialize && healthInitializedLife != LifeId;
        healthInitializedLife = LifeId;
        health.SetBaseStats(new HealthStat { maxHp = stat.maxHealth, hp = stat.maxHealth, defense = stat.defense }, fill);
        return health;
    }

    public void ReleaseHealthModule()
    {
        hasHealthModule = false;
        if (health == null) return;
        health.OnDead -= OnSummonDead;
        health.enabled = false;
    }

    private void OnSummonDead() => Despawn(true);
    public void TakeDamage(float damage) { if (CanAct && Health != null) Health.TakeDamage(damage); }

    public bool HasTransformed(SummonTransformEffect transformEffect) => completedTransforms.Contains(transformEffect);
    internal void RecordTransform(SummonTransformEffect transformEffect) => completedTransforms.Add(transformEffect);

    public SummonModification AddModification(SummonModificationSettings settings, ItemEffectContext source)
    {
        if (settings == null || !CanAct || (source != null && !source.CanContinue)) return null;
        SummonModification modification = new SummonModification(this, settings, source);
        modifications.Add(modification);
        extraLifetime += EffectStatUtility.Safe(settings.addRemainingLifetime, -600f, 600f, 0f);
        RefreshProfiles();
        return modification;
    }

    private float CurrentDamageMultiplier
    {
        get
        {
            float result = 1f;
            foreach (SummonModification modification in modifications)
                if (modification.IsValid(this)) result *= modification.DamageMultiplier;
            return EffectStatUtility.Safe(result, 0f, 1000000f, 1f);
        }
    }
    private float CurrentHealingMultiplier
    {
        get
        {
            float result = 1f;
            foreach (SummonModification modification in modifications)
                if (modification.IsValid(this)) result *= modification.HealingMultiplier;
            return EffectStatUtility.Safe(result, 0f, 1000000f, 1f);
        }
    }

    public float CalculateDamage(float damage)
    {
        if (executionContext != null && executionContext.damageOverride.HasValue) damage = executionContext.damageOverride.Value;
        float executionMultiplier = executionContext != null ? executionContext.damageMultiplier : 1f;
        return EffectStatUtility.Safe(damage * CurrentDamageMultiplier * executionMultiplier, 0f, 1000000f, 0f);
    }

    public void NotifyAttack()
    {
        if (!CanAct) return;
        AttackCount++;
        if (buffManager != null) buffManager.ConsumeSummonAttack(this);
        foreach (SummonModification modification in modifications) modification.ConsumeAttack();
        RefreshProfiles();
    }

    private void RefreshProfiles(bool force = false)
    {
        int life = LifeId;
        foreach (SummonModification expired in modifications.ToArray())
            if (modifications.Contains(expired) && !expired.IsValid(this))
            {
                bool completed = expired.HasCompletedNaturally(this);
                modifications.Remove(expired);
                expired.Dispose(completed);
                if (LifeId != life || finishing || !isActiveAndEnabled) return;
            }
        SummonBehaviourModule[] nextModules = modules;
        foreach (SummonModification modification in modifications)
        {
            if (modification.ReplaceModules) nextModules = modification.Modules;
        }
        if (!force && ReferenceEquals(effectiveModules, nextModules)) return;
        effectiveModules = nextModules;
        ProfileVersion++;
        if (initialized && isActiveAndEnabled) InitializeBehaviours();
    }

    public ItemData ResolveAttackItem(ItemData configured)
    {
        for (int i = modifications.Count - 1; i >= 0; i--)
            if (modifications[i].IsValid(this) && modifications[i].ReplaceAttackItem) return modifications[i].AttackItem;
        return configured;
    }

    private void ClearModifications()
    {
        SummonModification[] removed = modifications.ToArray();
        modifications.Clear();
        foreach (SummonModification modification in removed) modification.Dispose();
    }

    private void InitializeBehaviours()
    {
        DisposeBehaviours();
        if (effectiveModules == null) return;
        bool hasHealth = false;
        foreach (SummonBehaviourModule module in effectiveModules)
            if (module != null)
            {
                if (module is SummonHealthModule)
                {
                    if (hasHealth) continue; // 하나의 HP에 여러 기본 체력 스탯을 적용하지 않습니다.
                    hasHealth = true;
                }
                module.Prepare(CreateAttackContext(transform.position, Vector3.right));
                SummonBehaviourRuntime behaviour = module.CreateRuntime(this);
                if (behaviour != null) behaviours.Add(behaviour);
            }
        RefreshRuntimeInspector();
    }

    private void Update()
    {
        Advance(Time.deltaTime);
    }

    public void Advance(float deltaTime)
    {
        if (!initialized || finishing) return;
        if (!CanAct) { Clear(); return; }
        int life = LifeId;
        deltaTime = EffectStatUtility.Safe(deltaTime, 0f, 600f, 0f);
        foreach (SummonModification modification in modifications) modification.Tick(deltaTime);
        RefreshProfiles();
        if (LifeId != life || !CanAct) return;
        timer += deltaTime;
        if (timer >= spawnLifetime + extraLifetime)
        {
            Despawn(true);
            return;
        }
        int currentProfile = ProfileVersion;
        for (int i = 0; i < behaviours.Count; i++)
        {
            if (!CanAct) break;
            behaviours[i].Tick(deltaTime);
            if (ProfileVersion != currentProfile) break;
        }
    }

    public T CalculateModuleStat<T>(T baseStat, bool consumeUseCount = false) where T : class, IGameStat<T>
    {
        T current = buffManager != null
            ? buffManager.GetBuffedStatForTarget(baseStat, this, consumeUseCount: consumeUseCount, sourceItemData: sourceItemData)
            : BuffStatCloneUtility.Clone(baseStat);
        return executionContext != null ? EffectExecutionScaling.Apply(current, executionContext) : current;
    }

    public T GetRuntimeStat<T>(int moduleIndex) where T : class, IGameStat<T>
        => moduleIndex >= 0 && moduleIndex < behaviours.Count ? BuffStatCloneUtility.Clone(behaviours[moduleIndex].RuntimeStat as T) : null;

    public void RefreshBuffedStat()
    {
        foreach (SummonBehaviourRuntime behaviour in behaviours.ToArray()) behaviour.RefreshBuffedStat();
        RefreshRuntimeInspector();
    }
    private void RefreshRuntimeInspector()
    {
        runtimeModuleStats.Clear();
        foreach (SummonBehaviourRuntime behaviour in behaviours)
            if (behaviour.RuntimeStat != null)
                runtimeModuleStats.Add(new SummonModuleRuntimeView { module = behaviour.Module, currentStat = behaviour.RuntimeStat });
    }

    public void OnDynamicBuffChanged() => RefreshBuffedStat();
    private void RegisterDynamicBuffReceiver()
    {
        if (isActiveAndEnabled && buffManager != null) buffManager.RegisterDynamicBuffReceiver(this);
    }
    protected override void UnregisterDynamicBuffReceiverInternal()
    {
        if (buffManager != null) buffManager.UnregisterDynamicBuffReceiver(this);
    }

    private void RegisterBuffTarget()
    {
        if (registered || buffManager == null || !initialized || !isActiveAndEnabled) return;
        buffManager.RegisterBuffTarget(this);
        registered = true;
    }
    private void UnregisterBuffTarget()
    {
        if (registered && buffManager != null) buffManager.UnregisterBuffTarget(this);
        registered = false;
    }
    private void DisposeBehaviours()
    {
        SummonBehaviourRuntime[] removed = behaviours.ToArray();
        behaviours.Clear();
        runtimeModuleStats.Clear();
        foreach (SummonBehaviourRuntime behaviour in removed) behaviour.Dispose();
    }
    protected override void OnDisable()
    {
        groupReady = groupPendingReady = groupPendingActivation = false;
        unchecked { LifeId++; }
        if (health != null) health.OnDead -= OnSummonDead;
        ClearModifications();
        DisposeBehaviours();
        UnregisterBuffTarget();
        SummonRegistry.Unregister(this);
        SummonGroupRuntime.NotifyFieldChanged();
        base.OnDisable();
        ItemEffectLease cancelled = effectLease;
        effectLease = null;
        if (cancelled != null) cancelled.Cancel();
    }
    public void Despawn(bool completed)
    {
        if (finishing) return;
        finishing = true;
        if (completed)
        {
            ItemEffectLease completedLease = effectLease;
            effectLease = null;
            if (completedLease != null) completedLease.Finish();
        }
        gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }
    public override void Clear() => Despawn(false);
    public static void ClearAllActiveSummons()
    {
        var active = new List<SummonActor>(SummonRegistry.Active);
        foreach (SummonActor summon in active) if (summon != null) summon.Clear();
    }
}
