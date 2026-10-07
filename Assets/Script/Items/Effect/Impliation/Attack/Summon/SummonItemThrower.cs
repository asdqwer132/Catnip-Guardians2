using System.Collections.Generic;
using UnityEngine;

public enum SummonThrowTargetMode { NearestEnemy, RandomEnemy, RandomPosition }

// 기존 프리팹의 클래스/GUID와 버프 타깃을 유지하는 공통 소환수 호스트.
public class SummonItemThrower : AttackObject<SummonStat>, IBuffTarget
{
    private static readonly List<SummonItemThrower> activeThrowers = new List<SummonItemThrower>();
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
    private readonly List<SummonBehaviourRuntime> behaviours = new List<SummonBehaviourRuntime>();
    private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
    private ItemEffectContext executionContext;
    private float timer, throwTimer;
    private bool initialized, registered, finishing;

    public UnityEngine.Object BuffTargetObject => this;
    public string BuffTargetGroup => "Summon";
    public string BuffTargetDebugName => name;
    public float AttackPower => summonAttackPower;
    public float AttackRange => summonAttackRange;
    public float AttackInterval => summonThrowInterval;
    public bool CanAct => initialized && !finishing && isActiveAndEnabled &&
        (executionContext == null || executionContext.CanContinue);

    protected virtual void Awake()
    {
        if (itemThrowExecutor == null) itemThrowExecutor = GetComponent<ItemThrowExecutor>();
        if (rangeCollider == null) rangeCollider = GetComponent<CircleCollider2D>();
        ApplyRadius();
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        activeThrowers.Add(this);
        timer = throwTimer = 0f;
        finishing = false;
        if (initialized) InitializeBehaviours();
        RegisterBuffTarget();
    }

    private void Start()
    {
        // 씬에 직접 배치한 기존 프리팹도 직렬화된 스탯으로 동작한다.
        if (initialized) return;
        initialized = true;
        InitializeBehaviours();
    }

    public void ConfigureModules(SummonBehaviourModule[] overrides)
    {
        DisposeBehaviours();
        modules = overrides != null ? (SummonBehaviourModule[])overrides.Clone() : new SummonBehaviourModule[0];
        useLegacyThrowWhenNoModules = false;
        if (initialized) InitializeBehaviours();
    }

    public void SetExecutionContext(ItemEffectContext context)
        => executionContext = context != null ? context.Copy(transform.position, context.direction) : null;

    public ItemEffectContext CreateContext(Vector3 target, Vector3 direction)
    {
        if (executionContext != null)
        {
            ItemEffectContext result = executionContext.Copy(target, direction);
            result.usePosition = transform.position;
            return result;
        }
        return new ItemEffectContext(owner, sourceItemData, transform.position, target, sourceBag,
            buffManager: buffManager, direction: direction);
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
        ItemEffectContext context = CreateContext(target, (target - transform.position).normalized);
        if ((target - transform.position).sqrMagnitude <= 0.0001f)
        {
            ItemEffectExecutor.ExecuteItem(item, transform.position, target, context.direction,
                owner, sourceBag, buffManager, context, false, isThrownItem: true);
            return true;
        }
        return itemThrowExecutor.Throw(item, transform.position, target, owner, sourceBag, 0, context, false);
    }

    public override void InitWithSnapshotAndDynamicBuff(SummonStat snapshotAttackStat, ItemData sourceItemData,
        EquipmentBag sourceBag, BuffManager buffManager, GameObject owner)
    {
        UnregisterBuffTarget();
        DisposeBehaviours();
        timer = throwTimer = 0f;
        finishing = false;
        base.InitWithSnapshotAndDynamicBuff(snapshotAttackStat, sourceItemData, sourceBag, buffManager, owner);
        RegisterBuffTarget();
        initialized = true;
        InitializeBehaviours();
    }

    private void InitializeBehaviours()
    {
        DisposeBehaviours();
        if (modules == null) return;
        foreach (SummonBehaviourModule module in modules)
            if (module != null)
            {
                SummonBehaviourRuntime behaviour = module.CreateRuntime(this);
                if (behaviour != null) behaviours.Add(behaviour);
            }
    }

    private void Update()
    {
        if (!initialized || finishing) return;
        if (!CanAct) { Clear(); return; }
        timer += Time.deltaTime;
        if (timer >= lifeTime)
        {
            finishing = true;
            DisposeBehaviours();
            CompleteLifetime();
            Destroy(gameObject);
            return;
        }
        foreach (SummonBehaviourRuntime behaviour in behaviours.ToArray())
        {
            if (!CanAct) break;
            behaviour.Tick(Time.deltaTime);
        }
        if (behaviours.Count == 0 && useLegacyThrowWhenNoModules && CanAct)
        {
            throwTimer += Time.deltaTime;
            if (throwTimer >= AttackInterval)
            {
                throwTimer = 0f;
                Vector3 target;
                if (TryTarget(targetMode, out target)) ThrowItem(itemDatas, target);
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
        DisposeBehaviours();
        UnregisterBuffTarget();
        activeThrowers.Remove(this);
        base.OnDisable();
    }
    public static void ClearAllActiveThrowers()
    {
        foreach (SummonItemThrower summon in activeThrowers.ToArray()) if (summon != null) summon.Clear();
        activeThrowers.Clear();
    }
}
