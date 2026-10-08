using System;
using System.Collections.Generic;
using UnityEngine;

public enum HealthDefenseFormula { None, FlatReduction, PercentReduction }
public enum MaxHealthIncreasePolicy { KeepCurrentHp, AddIncrease, KeepRatio }
public enum HealthTeam { Unspecified, Ally, Enemy }

[Serializable]
public class HealthStat : IGameStat<HealthStat>
{
    [Header("Spawn")]
    public float hp = 1.5f;
    public float maxHp = 8f;
    [Tooltip("방어 공식이 Flat이면 고정 차감량, Percent이면 0~100 퍼센트입니다.")]
    public float defense;
    public HealthStat Clone() => (HealthStat)MemberwiseClone();
    public void Clamp()
    {
        maxHp = EffectStatUtility.Safe(maxHp, 1f, 100000000f, 1f);
        hp = EffectStatUtility.Safe(hp, 0f, maxHp, 0f);
        defense = EffectStatUtility.Safe(defense, 0f, 100000000f, 0f);
    }
}

[Serializable]
public class HealthDamagePolicy
{
    public bool canKill = true;
    public bool bypassDefense;
    public bool bypassShield;
    public bool bypassImmunity;
    [Tooltip("비용 차감은 끄면 피격 반응을 발생시키지 않습니다.")]
    public bool notifyDamaged = true;
    [NonSerialized] public ItemEffectContext sourceContext;
    [NonSerialized] public bool isTransferredDamage;
    public HealthDamagePolicy Clone() => (HealthDamagePolicy)MemberwiseClone();
}

public struct HealthDamageResult
{
    public float requestedDamage;
    public float damageAfterDefense;
    public float absorbedByShield;
    public float hpDamage;
    public bool killed;
    public ItemEffectContext sourceContext;
    public bool isTransferredDamage;
}

[BuffTargetGroups("Health", "PlantHealth", "SummonHealth")]
public class Health : MonoBehaviour, IBuffTarget
{
    private static readonly List<Health> active = new List<Health>();
    public static IReadOnlyList<Health> Active => active;
    [Header("Runtime HP")]
    public HealthStat currentHealthStat = new HealthStat();
    [SerializeField] private HealthStat baseHealthStat;
    [SerializeField] private BuffManager buffManager;
    private BuffManager registeredManager;
    [Header("Damage Rules")]
    [Tooltip("기획에서 공식이 확정되지 않았으므로 기본값은 방어 계산 없음입니다.")]
    public HealthDefenseFormula defenseFormula;
    public MaxHealthIncreasePolicy maxHealthIncreasePolicy;
    public bool damageImmune;
    public bool executionImmune;
    public bool executionRestricted;
    public HealthTeam team;

    private readonly List<HealthShieldState> shields = new List<HealthShieldState>();
    private ItemEffectContext damageSource;
    public ItemEffectContext CurrentDamageSource => damageSource;
    public float Hp => currentHealthStat != null ? currentHealthStat.hp : 0f;
    public float MaxHp => currentHealthStat != null ? currentHealthStat.maxHp : 0f;
    public float ShieldAmount
    {
        get
        {
            PurgeInvalidShields();
            float amount = 0f;
            for (int i = 0; i < shields.Count; i++) amount += shields[i].amount;
            return amount;
        }
    }
    public int LifeId { get; private set; }
    public bool IsDead { get; private set; }
    public bool AreStatusTimersStopped => TimeStopRuntime.IsStopped(TimeStopTargets.EnemyStatusTimers) &&
        (team == HealthTeam.Enemy || GetComponentInParent<Enemy>() != null);
    public event Action<float, float> OnHpChanged;
    public event Action<float> OnDamaged;
    public event Action<float> OnHealed;
    public event Action<float> OnShieldChanged;
    public event Action<HealthDamageResult> OnDamageResolved;
    public event Action OnDead;
    public UnityEngine.Object BuffTargetObject => this;
    [BuffTargetGroupName]
    public string buffTargetGroup = "Health";
    public string BuffTargetGroup => buffTargetGroup;
    public string BuffTargetDebugName => name;

    private void Awake() { EnsureStats(); }
    private void OnEnable()
    {
        if (!active.Contains(this)) active.Add(this);
        RegisterBuffTarget();
    }
    private void OnDisable()
    {
        active.Remove(this);
        unchecked { LifeId++; }
        ClearShields();
        UnregisterBuffTarget();
    }
    private void OnDestroy() { active.Remove(this); ClearShields(); UnregisterBuffTarget(); }
    private void Update() { TickShields(Time.deltaTime); }
    private void EnsureStats()
    {
        if (currentHealthStat == null) currentHealthStat = new HealthStat();
        if (baseHealthStat == null) baseHealthStat = currentHealthStat.Clone();
        baseHealthStat.Clamp();
        currentHealthStat.Clamp();
    }
    public void SetBaseDefense(float defense)
    {
        EnsureStats();
        baseHealthStat.defense = EffectStatUtility.Safe(defense, 0f, 100000000f, 0f);
        RefreshBuffedStat();
    }
    public void SetBuffManager(BuffManager manager)
    {
        if (buffManager != manager) UnregisterBuffTarget();
        buffManager = manager;
        RegisterBuffTarget();
    }
    private BuffManager Manager => buffManager != null ? buffManager : BuffManager.instance;
    private void RegisterBuffTarget()
    {
        BuffManager manager = Manager;
        if (!isActiveAndEnabled || manager == null || registeredManager == manager) return;
        UnregisterBuffTarget();
        registeredManager = manager;
        manager.RegisterBuffTarget(this);
    }
    private void UnregisterBuffTarget()
    {
        BuffManager manager = registeredManager;
        registeredManager = null;
        if (manager != null) manager.UnregisterBuffTarget(this);
    }
    public void RefreshBuffedStat()
    {
        EnsureStats();
        float previousHp = Hp;
        float previousMaxHp = MaxHp;
        BuffManager manager = Manager;
        HealthStat calculated = manager != null
            ? manager.GetBuffedStatForTarget(baseHealthStat, this)
            : baseHealthStat.Clone();
        ApplyBuffedStat(calculated, previousHp, previousMaxHp);
    }
    // HP is runtime state. Only max HP and defense are taken from the calculated stat.
    public void ApplyBuffedStat(HealthStat calculated)
    {
        EnsureStats();
        ApplyBuffedStat(calculated, Hp, MaxHp);
    }
    private void ApplyBuffedStat(HealthStat calculated, float previousHp, float previousMaxHp)
    {
        if (calculated == null) return;
        currentHealthStat = calculated.Clone();
        currentHealthStat.Clamp();
        float nextHp = previousHp;
        if (!IsDead && currentHealthStat.maxHp > previousMaxHp)
        {
            if (maxHealthIncreasePolicy == MaxHealthIncreasePolicy.AddIncrease)
                nextHp += currentHealthStat.maxHp - previousMaxHp;
            else if (maxHealthIncreasePolicy == MaxHealthIncreasePolicy.KeepRatio && previousMaxHp > 0f)
                nextHp *= currentHealthStat.maxHp / previousMaxHp;
        }
        currentHealthStat.hp = IsDead ? 0f : Mathf.Clamp(nextHp, 0f, currentHealthStat.maxHp);
        if (previousHp != Hp || previousMaxHp != MaxHp) BroadcastHpChanged();
    }
    public void Init(float startMaxHp, bool fillHp = true)
    {
        EnsureStats();
        float previousHp = Hp;
        unchecked { LifeId++; }
        ClearShields();
        UnregisterBuffTarget();
        baseHealthStat.maxHp = EffectStatUtility.Safe(startMaxHp, 1f, 100000000f, 1f);
        currentHealthStat = baseHealthStat.Clone();
        currentHealthStat.hp = fillHp ? currentHealthStat.maxHp : Mathf.Clamp(previousHp, 0f, currentHealthStat.maxHp);
        IsDead = false;
        BroadcastHpChanged();
        RegisterBuffTarget();
    }
    public void TakeDamage(float damage) { ApplyDamage(damage); }
    // Keeps existing actor/pattern damage entry points while carrying attack provenance.
    public IDisposable UseDamageSource(ItemEffectContext source)
    {
        DamageSourceScope scope = new DamageSourceScope(this, damageSource);
        damageSource = source;
        return scope;
    }
    private sealed class DamageSourceScope : IDisposable
    {
        private Health target;
        private readonly ItemEffectContext previous;
        public DamageSourceScope(Health target, ItemEffectContext previous) { this.target = target; this.previous = previous; }
        public void Dispose()
        {
            if (target != null) target.damageSource = previous;
            target = null;
        }
    }
    // Used by enemy lethal-pattern checks without consuming shields or emitting events.
    public float PreviewDamageToHp(float damage, HealthDamagePolicy policy = null)
    {
        EnsureStats();
        if (IsDead || damage <= 0f || float.IsNaN(damage) || float.IsInfinity(damage) ||
            (damageImmune && (policy == null || !policy.bypassImmunity))) return 0f;
        damage = ApplyDefense(damage, policy);
        if (policy == null || !policy.bypassShield)
        {
            float available = 0f;
            for (int i = 0; i < shields.Count; i++) if (shields[i].IsValid(this)) available += shields[i].amount;
            damage = Mathf.Max(0f, damage - available);
        }
        float minimumHp = policy == null || policy.canKill ? 0f : Mathf.Min(1f, Hp);
        return Mathf.Min(damage, Mathf.Max(0f, Hp - minimumHp));
    }
    private float ApplyDefense(float damage, HealthDamagePolicy policy)
    {
        if (policy != null && policy.bypassDefense) return damage;
        if (defenseFormula == HealthDefenseFormula.FlatReduction)
            return Mathf.Max(0f, damage - currentHealthStat.defense);
        if (defenseFormula == HealthDefenseFormula.PercentReduction)
            return damage * (1f - Mathf.Clamp01(currentHealthStat.defense / 100f));
        return damage;
    }
    public float ApplyDamage(float damage, HealthDamagePolicy policy = null)
    {
        EnsureStats();
        if (IsDead || float.IsNaN(damage) || float.IsInfinity(damage) || damage <= 0f) return 0f;
        int damagedLife = LifeId;
        bool bypassShield = policy != null && policy.bypassShield;
        bool bypassImmunity = policy != null && policy.bypassImmunity;
        bool canKill = policy == null || policy.canKill;
        if (damageImmune && !bypassImmunity) return 0f;
        ItemEffectContext source = policy != null && policy.sourceContext != null ? policy.sourceContext : damageSource;
        HealthDamageResult result = new HealthDamageResult
        {
            requestedDamage = damage,
            sourceContext = source != null ? source.Copy(source.targetPosition, source.direction) : null,
            isTransferredDamage = policy != null && policy.isTransferredDamage
        };
        damage = ApplyDefense(damage, policy);
        result.damageAfterDefense = damage;
        if (!bypassShield) result.absorbedByShield = AbsorbDamage(ref damage);
        if (LifeId != damagedLife || !isActiveAndEnabled || IsDead) return 0f;
        float minimumHp = canKill ? 0f : Mathf.Min(1f, Hp);
        float previousHp = Hp;
        currentHealthStat.hp = Mathf.Clamp(Hp - damage, minimumHp, MaxHp);
        result.hpDamage = previousHp - Hp;
        result.killed = Hp <= 0f;
        if (result.killed) IsDead = true;
        if (result.hpDamage > 0f)
        {
            if (policy == null || policy.notifyDamaged) OnDamaged?.Invoke(result.hpDamage);
            if (LifeId != damagedLife || !isActiveAndEnabled) return result.hpDamage;
            BroadcastHpChanged();
            if (LifeId != damagedLife || !isActiveAndEnabled) return result.hpDamage;
        }
        OnDamageResolved?.Invoke(result);
        if (LifeId != damagedLife || !isActiveAndEnabled) return result.hpDamage;
        if (result.killed)
        {
            ClearShields();
            OnDead?.Invoke();
        }
        return result.hpDamage;
    }
    public void Heal(float amount) { ApplyHealing(amount); }
    public float ApplyHealing(float amount)
    {
        EnsureStats();
        if (IsDead || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return 0f;
        float previousHp = Hp;
        currentHealthStat.hp = Mathf.Clamp(Hp + amount, 0f, MaxHp);
        float healed = Hp - previousHp;
        if (healed > 0f)
        {
            OnHealed?.Invoke(healed);
            BroadcastHpChanged();
        }
        return healed;
    }
    public void AddShield(float amount, float duration, ShieldReapplyMode mode,
        UnityEngine.Object source, ItemEffectContext context, EffectVisualData endVisual = null)
    {
        if (IsDead || !isActiveAndEnabled || context == null || !context.CanContinue ||
            amount <= 0f || duration <= 0f || float.IsNaN(amount) || float.IsInfinity(amount) ||
            float.IsNaN(duration) || float.IsInfinity(duration)) return;
        PurgeInvalidShields();
        HealthShieldState state = null;
        for (int i = 0; i < shields.Count; i++)
            if (shields[i].source == source && shields[i].owner == context.owner) { state = shields[i]; break; }
        if (state == null)
        {
            state = new HealthShieldState { source = source, owner = context.owner, context = context, lifeId = LifeId };
            shields.Add(state);
        }
        state.amount = mode == ShieldReapplyMode.Add ? Mathf.Min(100000000f, state.amount + amount) : amount;
        state.remaining = duration;
        state.context = context;
        state.completion.Track(context, endVisual, transform);
        NotifyShieldChanged();
    }
    public void TickShields(float deltaTime)
    {
        if (shields.Count == 0) return;
        if (AreStatusTimersStopped) { PurgeInvalidShields(); return; }
        deltaTime = EffectStatUtility.Safe(deltaTime, 0f, 600f, 0f);
        int updatingLife = LifeId;
        bool changed = false;
        HealthShieldState[] updating = shields.ToArray();
        for (int i = 0; i < updating.Length; i++)
        {
            HealthShieldState state = updating[i];
            if (!shields.Contains(state)) continue;
            state.remaining -= Mathf.Max(0f, deltaTime);
            bool valid = state.IsValid(this);
            if (valid && state.remaining > 0f && state.amount > 0f) continue;
            shields.Remove(state);
            state.completion.Finish(valid);
            changed = true;
            if (LifeId != updatingLife) return;
        }
        if (changed) NotifyShieldChanged();
    }
    private void PurgeInvalidShields()
    {
        if (shields.Count == 0) return;
        int updatingLife = LifeId;
        bool changed = false;
        HealthShieldState[] updating = shields.ToArray();
        for (int i = 0; i < updating.Length; i++)
        {
            HealthShieldState state = updating[i];
            if (!shields.Contains(state) || state.IsValid(this)) continue;
            shields.Remove(state);
            state.completion.Finish(false);
            changed = true;
            if (LifeId != updatingLife) return;
        }
        if (changed) NotifyShieldChanged();
    }
    private float AbsorbDamage(ref float damage)
    {
        PurgeInvalidShields();
        if (shields.Count == 0) return 0f;
        int absorbingLife = LifeId;
        float absorbed = 0f;
        HealthShieldState[] eligible = shields.ToArray();
        float[] amounts = new float[eligible.Length];
        for (int i = 0; i < eligible.Length; i++) amounts[i] = eligible[i].amount;
        for (int i = 0; i < eligible.Length && damage > 0f; i++)
        {
            HealthShieldState state = eligible[i];
            if (!shields.Contains(state) || !state.IsValid(this)) continue;
            float used = Mathf.Min(Mathf.Min(state.amount, amounts[i]), damage);
            damage -= used;
            state.amount -= used;
            absorbed += used;
            if (state.amount > 0f) continue;
            shields.Remove(state);
            state.completion.Finish(true);
            if (LifeId != absorbingLife || IsDead || !isActiveAndEnabled) break;
        }
        if (absorbed > 0f) NotifyShieldChanged();
        return absorbed;
    }
    private void ClearShields()
    {
        if (shields.Count == 0) return;
        HealthShieldState[] removed = shields.ToArray();
        shields.Clear();
        for (int i = 0; i < removed.Length; i++) removed[i].completion.Finish(false);
        NotifyShieldChanged();
    }
    private void NotifyShieldChanged()
    {
        float total = 0f;
        for (int i = 0; i < shields.Count; i++) total += shields[i].amount;
        OnShieldChanged?.Invoke(total);
    }
    private void BroadcastHpChanged() { OnHpChanged?.Invoke(Hp, MaxHp); }
}

internal sealed class HealthShieldState
{
    public float amount;
    public float remaining;
    public int lifeId;
    public UnityEngine.Object source;
    public GameObject owner;
    public ItemEffectContext context;
    public readonly ItemEffectCompletionGroup completion = new ItemEffectCompletionGroup();
    public bool IsValid(Health target) => target != null && target.isActiveAndEnabled && !target.IsDead &&
        lifeId == target.LifeId && context != null && context.CanContinue;
}
