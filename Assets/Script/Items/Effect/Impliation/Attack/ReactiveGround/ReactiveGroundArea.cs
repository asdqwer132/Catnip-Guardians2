using System.Collections.Generic;
using UnityEngine;

public sealed class ReactiveGroundArea : AttackObject<ReactiveGroundStat>
{
    private static readonly List<ReactiveGroundArea> active = new List<ReactiveGroundArea>();
    private ItemEffectContext context;
    private ReactiveGroundStat stat;
    private ItemEffectData[] defaults, specials;
    private ItemData[] accepted;
    private Transform visual;
    private Vector3 visualScale;
    private bool sameOwner, replaceIncoming, scaleVisual, initialized, reacting, finishing;
    private float elapsed, tickElapsed, nextReaction, specialUntil;
    private AreaDefinition definition;
    private ItemEffectData[] startEffects, enterEffects, tickEffects, exitEffects, endEffects, residenceEffects;
    private readonly EnemyQueryBuffer targetQuery = new EnemyQueryBuffer();
    private readonly Dictionary<Enemy, Residence> residents = new Dictionary<Enemy, Residence>();
    private readonly HashSet<Enemy> inside = new HashSet<Enemy>();
    private LayerMask targetMask;
    private float residenceThreshold;
    private int maxResidenceTriggers;
    private bool cumulativeResidence, resetResidenceOnExit, removeAfterResidence;
    private sealed class Residence
    {
        public int life, triggers;
        public bool inside;
        public float duration;
        public Vector3 lastPosition;
    }

    public bool IsSpecialActive => initialized && Time.time < specialUntil;
    public AreaDefinition Definition => definition;
    public bool IsRegistered => initialized && !finishing && isActiveAndEnabled &&
        context != null && context.CanContinue && stat != null && elapsed < stat.groundLifetime;
    public float RemainingLifetime => stat != null ? Mathf.Max(0f, stat.groundLifetime - elapsed) : 0f;
    public bool Contains(Vector3 position) => IsRegistered &&
        ((Vector2)(position - transform.position)).sqrMagnitude <= stat.groundRadius * stat.groundRadius;

    public void Init(ReactiveGroundEffect effect, ItemEffectContext execution)
    {
        if (effect == null || execution == null || effect.BaseStat == null) return;
        if (initialized) return;
        context = execution.Copy(transform.position, execution.direction);
        definition = effect.areaDefinition;
        if (definition != null)
        {
            startEffects = ItemEffectUtility.Copy(definition.onStart);
            enterEffects = ItemEffectUtility.Copy(definition.onEnter);
            tickEffects = ItemEffectUtility.Copy(definition.onTick);
            exitEffects = ItemEffectUtility.Copy(definition.onExit);
            endEffects = ItemEffectUtility.Copy(definition.onNaturalEnd);
            residenceEffects = ItemEffectUtility.Copy(definition.onResidence);
            targetMask = definition.targetMask;
            residenceThreshold = EffectStatUtility.Safe(definition.residenceThreshold, 0f, 600f, 0f);
            maxResidenceTriggers = Mathf.Max(1, definition.maxResidenceTriggersPerTarget);
            cumulativeResidence = definition.cumulativeResidence;
            resetResidenceOnExit = definition.resetResidenceOnExit;
            removeAfterResidence = definition.removeAfterResidenceTrigger;
        }
        defaults = ItemEffectUtility.Copy(effect.defaultEffects);
        specials = ItemEffectUtility.Copy(effect.specialEffects);
        accepted = effect.acceptedItems != null ? (ItemData[])effect.acceptedItems.Clone() : null;
        sameOwner = effect.onlySameOwner;
        replaceIncoming = effect.replaceIncomingItemEffects;
        scaleVisual = effect.scaleVisualByRadius;
        GameObject visualPrefab = effect.areaVisualPrefab != null ? effect.areaVisualPrefab :
            (definition != null ? definition.visualPrefab : null);
        if (visualPrefab != null)
        {
            visual = Instantiate(visualPrefab, transform).transform;
            visual.localPosition = Vector3.zero;
            visualScale = visual.localScale;
        }
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetUnscaledSnapshotStat(effect, effect.BaseStat),
            context.sourceItemData, context.sourceBag, context.buffManager, context.owner);
        initialized = true;
        active.Add(this);
        AreaRegistry.Register(this);
        ItemEffectUtility.Execute(startEffects, context.Copy(transform.position, context.direction));
        if (!IsRegistered) return;
        ItemEffectUtility.Execute(defaults, context.Copy(transform.position, context.direction));
        if (definition != null && IsRegistered) UpdateResidents(0f);
    }

    protected override void ApplyStat(ReactiveGroundStat current)
    {
        stat = current.Clone();
        stat.Clamp();
        if (visual != null && scaleVisual)
            visual.localScale = Vector3.Scale(visualScale, new Vector3(stat.groundRadius * 2f, stat.groundRadius * 2f, 1f));
    }

    private void Update()
    {
        if (!initialized || finishing) return;
        if (!context.CanContinue) { Clear(); return; }
        elapsed += Time.deltaTime;
        if (elapsed >= stat.groundLifetime)
        {
            finishing = true;
            active.Remove(this);
            AreaRegistry.Unregister(this);
            ItemEffectUtility.Execute(endEffects, context.Copy(transform.position, context.direction));
            CompleteLifetime();
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }
        if (definition != null) UpdateResidents(Time.deltaTime);
        if (!IsRegistered) return;
        tickElapsed += Time.deltaTime;
        if (tickElapsed < stat.groundTickInterval) return;
        tickElapsed = 0f;
        ItemEffectUtility.Execute(IsSpecialActive ? specials : defaults,
            context.Copy(transform.position, context.direction));
        if (IsRegistered) ItemEffectUtility.Execute(tickEffects, context.Copy(transform.position, context.direction));
    }

    public void Consume()
    {
        if (!IsRegistered) return;
        finishing = true;
        AreaRegistry.Unregister(this);
        active.Remove(this);
        Clear();
    }

    public override void Clear()
    {
        finishing = true;
        AreaRegistry.Unregister(this);
        active.Remove(this);
        if (gameObject == null) return;
        gameObject.SetActive(false);
        if (Application.isPlaying) Destroy(gameObject);
        else DestroyImmediate(gameObject);
    }

    private void ExecuteForEnemy(ItemEffectData[] effects, Enemy enemy, Vector3 position, int life)
    {
        if (!context.CanContinue) return;
        ItemEffectContext targetContext = context.Copy(position, context.direction);
        targetContext.hitTarget = enemy;
        targetContext.hitTargetLifeId = life;
        ItemEffectUtility.Execute(effects, targetContext);
    }

    private void UpdateResidents(float deltaTime)
    {
        targetQuery.Scan(transform.position, stat.groundRadius, targetMask);
        inside.Clear();
        foreach (Enemy enemy in targetQuery.Enemies)
        {
            if (!IsRegistered) return;
            if (enemy == null || !enemy.CanReceiveHitEffects) continue;
            inside.Add(enemy);
            Residence residence;
            if (!residents.TryGetValue(enemy, out residence) || residence.life != enemy.HitEffectLifeId)
            {
                residence = new Residence { life = enemy.HitEffectLifeId };
                residents[enemy] = residence;
            }
            residence.lastPosition = enemy.transform.position;
            if (!residence.inside)
            {
                residence.inside = true;
                ExecuteForEnemy(enterEffects, enemy, residence.lastPosition, residence.life);
            }
            if (!IsRegistered || enemy == null || !enemy.CanReceiveHitEffects || enemy.HitEffectLifeId != residence.life)
                continue;
            residence.duration += Mathf.Max(0f, deltaTime);
            if (residenceThreshold <= 0f || residence.triggers >= maxResidenceTriggers ||
                residence.duration < residenceThreshold) continue;
            residence.triggers++;
            residence.duration -= residenceThreshold;
            ExecuteForEnemy(residenceEffects, enemy, residence.lastPosition, residence.life);
            if (removeAfterResidence) { Consume(); return; }
        }
        foreach (KeyValuePair<Enemy, Residence> entry in new List<KeyValuePair<Enemy, Residence>>(residents))
        {
            if (!IsRegistered) return;
            Enemy enemy = entry.Key;
            Residence residence = entry.Value;
            if (inside.Contains(enemy)) continue;
            if (residence.inside)
            {
                residence.inside = false;
                if (enemy != null && enemy.CanReceiveHitEffects && enemy.HitEffectLifeId == residence.life)
                    ExecuteForEnemy(exitEffects, enemy, enemy.transform.position, residence.life);
                if (!cumulativeResidence || resetResidenceOnExit) residence.duration = 0f;
            }
            if (enemy == null || !enemy.CanReceiveHitEffects || enemy.HitEffectLifeId != residence.life)
                residents.Remove(enemy);
        }
    }

    // 착지 전에 영역 목록을 복사한다. 이번 착지로 새로 생성된 장판은 같은 착지에 반응하지 않는다.
    public static bool NotifyItemLanded(ItemEffectContext incoming)
    {
        if (incoming == null || !incoming.CanContinue) return false;
        bool replace = false;
        foreach (ReactiveGroundArea area in active.ToArray())
        {
            if (!incoming.CanContinue) break;
            if (area != null && area.TryReact(incoming)) replace |= area.replaceIncoming;
        }
        return replace;
    }

    private bool TryReact(ItemEffectContext incoming)
    {
        if (!initialized || finishing || reacting || !isActiveAndEnabled || !context.CanContinue ||
            elapsed >= stat.groundLifetime || Time.time < nextReaction) return false;
        if (sameOwner && incoming.owner != owner) return false;
        if (((Vector2)(incoming.targetPosition - transform.position)).sqrMagnitude > stat.groundRadius * stat.groundRadius)
            return false;
        if (accepted != null && accepted.Length > 0 && System.Array.IndexOf(accepted, incoming.sourceItemData) < 0)
            return false;
        nextReaction = Time.time + stat.groundReactionCooldown;
        specialUntil = Time.time + stat.groundSpecialDuration;
        tickElapsed = 0f;
        reacting = true;
        try
        {
            // 피해/버프의 출처는 장판 생성자다. 착지 아이템은 acceptedItems 필터에 사용한다.
            ItemEffectUtility.Execute(specials, context.Copy(transform.position, incoming.direction));
        }
        finally { reacting = false; }
        return true;
    }

    protected override void OnDisable()
    {
        initialized = false;
        active.Remove(this);
        AreaRegistry.Unregister(this);
        residents.Clear();
        base.OnDisable();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (stat != null) Gizmos.DrawWireSphere(transform.position, stat.groundRadius);
    }
#endif
}
