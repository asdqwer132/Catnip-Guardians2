using System.Collections.Generic;
using UnityEngine;

public enum SummonThrowTargetMode { NearestEnemy, RandomEnemy, RandomPosition }

// 기존 프리팹의 클래스/GUID와 버프 타깃을 유지하는 공통 소환수 호스트.
public class SummonItemThrower : AttackObject<SummonStat>, IBuffTarget, IDamageable
{
    [Header("Identity / Health")]
    public SummonDefinition definition;
    [Tooltip("켜면 아이템 투척 피해를 summonAttackPower로 지정합니다. 끄면 아이템 자체 피해를 사용합니다.")]
    public bool overrideThrownItemDamage;
    [Header("Component")]
    public CircleCollider2D rangeCollider;
    public Transform rangeVisual;
    [Header("Behaviour Modules")]
    [Tooltip("행동 에셋을 원하는 순서로 조합합니다. 이동 모듈을 공격 모듈보다 앞에 배치하세요.")]
    public SummonBehaviourModule[] modules;
    [Tooltip("모듈 목록이 비어 있으면 기존 아이템 투척 설정으로 동작합니다.")]
    public bool useLegacyThrowWhenNoModules = true;
    [Header("Legacy Throw")]
    public ItemThrowExecutor itemThrowExecutor;
    public ItemData itemDatas;
    public SummonThrowTargetMode targetMode = SummonThrowTargetMode.NearestEnemy;
    [Header("Detect")]
    public LayerMask enemyLayerMask;
    [Header("Runtime Stat")]
    [SerializeField] private float summonAttackPower;
    [SerializeField] private float summonAttackRange = 5f;
    [SerializeField] private float summonThrowInterval = 1f;
    [SerializeField] private float lifeTime = 5f;
    private float damageMultiplier = 1f, healingMultiplier = 1f, extraLifetime;
    private readonly List<SummonBehaviourRuntime> behaviours = new List<SummonBehaviourRuntime>();
    private readonly List<SummonModification> modifications = new List<SummonModification>();
    private readonly HashSet<SummonTransformEffect> completedTransforms = new HashSet<SummonTransformEffect>();
    private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
    private ItemEffectContext executionContext;
    private SummonBehaviourModule[] effectiveModules;
    private ItemData effectiveItem;
    private bool effectiveLegacyThrow;
    private Health health;
    private float timer, throwTimer;
    private bool initialized, registered, finishing;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "Summon";
    public string BuffTargetDebugName => name;
    public float AttackPower => summonAttackPower;
    public float AttackRange => summonAttackRange;
    public float AttackInterval => summonThrowInterval;
    public GameObject Owner => owner;
    public SummonDefinition Definition => definition;
    public Health Health => health != null ? health : GetComponent<Health>();
    public Transform DamageTransform => transform;
    // HP 없는 일반 공격 소환물은 적의 피격 대상으로 선택하지 않는다.
    public bool IsDead => finishing || Health == null || Health.IsDead;
    public int LifeId { get; private set; }
    public int ProfileVersion { get; private set; }
    public int AttackCount { get; private set; }
    public IReadOnlyList<SummonBehaviourModule> ActiveModules => effectiveModules ?? new SummonBehaviourModule[0];
    public ItemData ActiveAttackItem => effectiveItem;
    public float RemainingLifeTime => Mathf.Max(0f, lifeTime + extraLifetime - timer);
    public bool IsTransformReserved { get; internal set; }
    public bool CanAct => initialized && !finishing && isActiveAndEnabled &&
        (Health == null || !Health.IsDead) && (executionContext == null || executionContext.CanContinue);

    protected virtual void Awake()
    {
        if (itemThrowExecutor == null) itemThrowExecutor = GetComponent<ItemThrowExecutor>();
        if (rangeCollider == null) rangeCollider = GetComponent<CircleCollider2D>();
        ApplyRadius();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        unchecked { LifeId++; }
        SummonRegistry.Register(this);
        timer = throwTimer = 0f;
        extraLifetime = 0f;
        AttackCount = 0;
        completedTransforms.Clear();
        IsTransformReserved = false;
        finishing = false;
        if (initialized)
        {
            if (useSnapshotAndDynamicBuff) { RegisterDynamicBuffReceiver(); OnDynamicBuffChanged(); }
            InitializeHealth();
            RefreshProfiles(true);
        }
        RegisterBuffTarget();
    }

    private void Start()
    {
        // 씬에 직접 배치한 기존 프리팹도 직렬화된 스탯으로 동작한다.
        if (initialized) return;
        initialized = true;
        InitializeHealth();
        RefreshProfiles(true);
    }

    public void ConfigureModules(SummonBehaviourModule[] overrides)
    {
        modules = overrides != null ? (SummonBehaviourModule[])overrides.Clone() : new SummonBehaviourModule[0];
        useLegacyThrowWhenNoModules = false;
        if (initialized) RefreshProfiles(true);
    }

    public void ConfigureDefinition(SummonDefinition value) { definition = value; }

    public void SetExecutionContext(ItemEffectContext context)
        => executionContext = context != null ? context.Copy(transform.position, context.direction) : null;

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

    public ItemEffectContext CreateAttackContext(Vector3 target, Vector3 direction)
    {
        ItemEffectContext context = CreateContext(target, direction);
        context.damageMultiplier *= CurrentDamageMultiplier;
        context.healingMultiplier *= CurrentHealingMultiplier;
        return context;
    }

    public bool TryTarget(SummonThrowTargetMode mode, out Vector3 target)
    {
        target = transform.position;
        if (mode == SummonThrowTargetMode.RandomPosition)
        { target += (Vector3)(Random.insideUnitCircle * AttackRange); return true; }
        query.Scan(transform.position, AttackRange, enemyLayerMask);
        Enemy enemy = EnemyQueryBuffer.Select(query.Enemies, transform.position, mode == SummonThrowTargetMode.RandomEnemy);
        if (enemy == null) return false;
        target = enemy.transform.position;
        return true;
    }

    public bool ThrowItem(ItemData item, Vector3 target)
    {
        if (!CanAct || item == null) return false;
        if (itemThrowExecutor == null)
        {
            ItemEffectExecutor executor = gameObject.AddComponent<ItemEffectExecutor>();
            executor.buffManager = buffManager;
            itemThrowExecutor = gameObject.AddComponent<ItemThrowExecutor>();
            itemThrowExecutor.itemEffectExecutor = executor;
            itemThrowExecutor.showTargetRange = false;
        }
        ItemEffectContext context = CreateAttackContext(target, (target - transform.position).normalized);
        if (overrideThrownItemDamage) context.damageOverride = AttackPower;
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

    public override void InitWithSnapshotAndDynamicBuff(SummonStat snapshotAttackStat, ItemData sourceItemData,
        EquipmentBag sourceBag, BuffManager buffManager, GameObject owner)
    {
        UnregisterBuffTarget();
        ClearModifications();
        DisposeBehaviours();
        unchecked { LifeId++; }
        completedTransforms.Clear();
        IsTransformReserved = false;
        AttackCount = 0;
        extraLifetime = 0f;
        timer = throwTimer = 0f;
        finishing = false;
        base.InitWithSnapshotAndDynamicBuff(snapshotAttackStat, sourceItemData, sourceBag, buffManager, owner);
        RegisterBuffTarget();
        initialized = true;
        InitializeHealth();
        RefreshProfiles(true);
    }

    private void InitializeHealth()
    {
        if (health != null) health.OnDead -= OnSummonDead;
        health = GetComponent<Health>();
        if (health == null && definition != null && definition.enableHealth) health = gameObject.AddComponent<Health>();
        if (health == null) return;
        health.team = HealthTeam.Ally;
        health.buffTargetGroup = "SummonHealth";
        health.SetBuffManager(buffManager);
        float maxHp = definition != null && definition.enableHealth ? definition.maxHealth : health.MaxHp;
        health.Init(EffectStatUtility.Safe(maxHp, 0.01f, 1000000f, 10f));
        health.OnDead += OnSummonDead;
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
            float result = damageMultiplier;
            foreach (SummonModification modification in modifications)
                if (modification.IsValid(this)) result *= modification.DamageMultiplier;
            return EffectStatUtility.Safe(result, 0f, 1000000f, 1f);
        }
    }
    private float CurrentHealingMultiplier
    {
        get
        {
            float result = healingMultiplier;
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
        effectiveItem = itemDatas;
        effectiveLegacyThrow = useLegacyThrowWhenNoModules;
        foreach (SummonModification modification in modifications)
        {
            if (modification.ReplaceModules) { nextModules = modification.Modules; effectiveLegacyThrow = false; }
            if (modification.ReplaceAttackItem) effectiveItem = modification.AttackItem;
        }
        if (!force && ReferenceEquals(effectiveModules, nextModules)) return;
        effectiveModules = nextModules;
        ProfileVersion++;
        if (initialized) InitializeBehaviours();
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
        foreach (SummonBehaviourModule module in effectiveModules)
            if (module != null)
            {
                module.Prepare(CreateAttackContext(transform.position, Vector3.right));
                SummonBehaviourRuntime behaviour = module.CreateRuntime(this);
                if (behaviour != null) behaviours.Add(behaviour);
            }
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
        if (timer >= lifeTime + extraLifetime)
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
        if (behaviours.Count == 0 && effectiveLegacyThrow && CanAct)
        {
            throwTimer += deltaTime;
            if (throwTimer >= AttackInterval)
            {
                throwTimer = 0f;
                Vector3 target;
                if (TryTarget(targetMode, out target)) ThrowItem(effectiveItem, target);
            }
        }
    }

    protected override void RefreshStatFromSnapshotAndDynamicBuff()
    {
        if (snapshotAttackStat == null) return;
        SummonStat current = snapshotAttackStat;
        if (buffManager != null)
        {
            current = buffManager.GetBuffedStatForItem(current, sourceItemData, sourceBag,
                BuffCalculationMode.DynamicOnly) ?? current;
            current = buffManager.GetBuffedStatForTarget(current, this) ?? current;
        }
        ApplyStat(current);
    }

    public void RefreshBuffedStat() => RefreshStatFromSnapshotAndDynamicBuff();

    protected override void ApplyStat(SummonStat current)
    {
        SummonStat stat = current.Clone();
        stat.Clamp();
        summonAttackPower = stat.summonAttackPower;
        summonAttackRange = stat.summonAttackRange;
        summonThrowInterval = stat.summonThrowInterval;
        lifeTime = stat.summonLifeTime;
        damageMultiplier = stat.summonDamageMultiplier;
        healingMultiplier = stat.summonHealingMultiplier;
        ApplyRadius();
    }

    private void ApplyRadius()
    {
        if (rangeVisual != null && rangeVisual != transform)
            rangeVisual.localScale = new Vector3(summonAttackRange * 2f, summonAttackRange * 2f, 1f);
        if (rangeCollider != null)
        { rangeCollider.radius = summonAttackRange; rangeCollider.isTrigger = true; }
    }

    private void RegisterBuffTarget()
    {
        if (registered || buffManager == null) return;
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
        foreach (SummonBehaviourRuntime behaviour in behaviours) behaviour.Dispose();
        behaviours.Clear();
    }
    protected override void OnDisable()
    {
        unchecked { LifeId++; }
        if (health != null) health.OnDead -= OnSummonDead;
        ClearModifications();
        DisposeBehaviours();
        UnregisterBuffTarget();
        SummonRegistry.Unregister(this);
        base.OnDisable();
    }
    public void Despawn(bool completed)
    {
        if (finishing) return;
        finishing = true;
        if (completed) CompleteLifetime();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
    public override void Clear() => Despawn(false);
    public static void ClearAllActiveThrowers()
    {
        var active = new List<SummonItemThrower>(SummonRegistry.Active);
        foreach (SummonItemThrower summon in active) if (summon != null) summon.Clear();
    }
}
