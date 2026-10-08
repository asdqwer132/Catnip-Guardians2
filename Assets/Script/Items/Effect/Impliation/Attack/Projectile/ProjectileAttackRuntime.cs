using System.Collections.Generic;
using UnityEngine;

// Only this attack root moves. A visual prefab is its child and needs no attack script.
public sealed class ProjectileAttackRuntime : AttackObject<ProjectileAttackStat>
{
    private struct HitRecord { public int cycle; public float time; }
    private readonly Dictionary<EnemyLifeKey, HitRecord> hitRecords = new Dictionary<EnemyLifeKey, HitRecord>();
    private readonly EnemyQueryBuffer query = new EnemyQueryBuffer();
    private readonly EnemyQueryBuffer targetQuery = new EnemyQueryBuffer();
    private ProjectileAttackEffect data;
    private ProjectileAttackStat stat;
    private ItemEffectContext context;
    private HitEffectDispatcher dispatcher;
    private Vector3 origin, direction;
    private Enemy target;
    private int targetLife, cycle, totalHits;
    private float elapsed, distance, orbitAngle, orbitTravel;
    private bool initialized, finished, returning;

    public void Init(ProjectileAttackEffect effect, ItemEffectContext source)
    {
        data = effect; context = source.Copy(source.targetPosition, source.direction);
        origin = context.targetPosition;
        if (!context.TryGetDirection(out direction)) direction = Vector3.right;
        dispatcher = new HitEffectDispatcher(data.onHitEffects, data.hitEffectApplyMode, context);
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetUnscaledSnapshotStat(data, data.attackStat), context.sourceItemData,
            context.sourceBag, context.buffManager, context.owner);
        orbitAngle = EffectStatUtility.Safe(data.orbitStartAngle, -36000f, 36000f, 0f) * Mathf.Deg2Rad;
        if (data.path == ProjectileAttackPath.Orbit)
            transform.position = Center() + OrbitOffset(orbitAngle);
        if (data.projectileSprite != null)
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = data.projectileSprite;
        }
        if (data.path == ProjectileAttackPath.Homing) SelectTarget();
        initialized = true;
    }
    protected override void ApplyStat(ProjectileAttackStat currentStat) { stat = currentStat; }

    private void Update() => Tick(Time.deltaTime);

    public void Tick(float deltaTime)
    {
        if (!initialized || finished) return;
        if (!context.CanContinue || stat == null) { Finish(false); return; }
        float dt = Mathf.Min(Mathf.Max(0f, deltaTime), Mathf.Max(0f, stat.projectileLifetime - elapsed));
        elapsed += dt;
        if (data.path == ProjectileAttackPath.Orbit) TickOrbit(dt);
        else if (data.path == ProjectileAttackPath.Return) TickReturn(dt);
        else TickForward(dt);
        if (!finished && elapsed >= stat.projectileLifetime) Finish(true);
    }

    private void TickForward(float dt)
    {
        if (data.path == ProjectileAttackPath.Homing)
        {
            if (target == null || target.HitEffectLifeId != targetLife || !target.CanReceiveHitEffects)
            {
                target = null;
                if (data.retargetWhenTargetLost) SelectTarget();
            }
            if (target != null)
            {
                Vector3 desired = target.transform.position - transform.position;
                if (desired.sqrMagnitude > 0.000001f)
                {
                    float current = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                    float goal = Mathf.Atan2(desired.y, desired.x) * Mathf.Rad2Deg;
                    float angle = Mathf.MoveTowardsAngle(current, goal,
                        EffectStatUtility.Safe(data.homingTurnSpeed, 0f, 100000f, 360f) * dt);
                    direction = Quaternion.Euler(0f, 0f, angle) * Vector3.right;
                }
            }
        }
        float step = Mathf.Min(stat.projectileSpeed * dt, Mathf.Max(0f, stat.projectileDistance - distance));
        MoveAndHit(transform.position + direction * step, 0);
        distance += step;
        if (!finished && distance >= stat.projectileDistance) Finish(true);
    }

    private void TickReturn(float dt)
    {
        float travel = stat.projectileSpeed * dt;
        if (!returning)
        {
            float step = Mathf.Min(travel, Mathf.Max(0f, stat.projectileDistance - distance));
            MoveAndHit(transform.position + direction * step, 0);
            if (finished) return;
            distance += step; travel -= step;
            if (distance >= stat.projectileDistance) { returning = true; cycle = 1; }
        }
        if (!returning || finished || travel <= 0f) return;
        Vector3 end = data.returnToOwner && context.owner != null ? context.owner.transform.position : origin;
        end.z = 0f;
        Vector3 delta = end - transform.position;
        float remaining = delta.magnitude;
        if (remaining <= 0.0001f) { Finish(true); return; }
        float returnStep = Mathf.Min(travel, remaining);
        MoveAndHit(transform.position + delta.normalized * returnStep, 1);
        if (!finished && returnStep >= remaining) Finish(true);
    }

    private Vector3 Center()
    {
        Vector3 center = data.orbitCenter == ProjectileOrbitCenter.OwnerPosition && context.owner != null
            ? context.owner.transform.position : origin;
        center.z = 0f;
        return center;
    }
    private Vector3 OrbitOffset(float angle)
        => new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * stat.projectileOrbitRadius;

    private void TickOrbit(float dt)
    {
        // Small swept chords prevent a fast orbit from skipping the circle's edge.
        // A hard per-frame budget keeps extreme data from creating an unbounded loop.
        float angular = Mathf.Min(stat.projectileSpeed * dt / stat.projectileOrbitRadius, Mathf.PI * 16f);
        int segments = Mathf.Clamp(Mathf.CeilToInt(angular / (Mathf.PI / 12f)), 1, 192);
        float remaining = angular;
        int budget = segments + 16;
        while (remaining > 0.000001f && !finished && budget-- > 0)
        {
            float cycleRemaining = Mathf.PI * 2f - orbitTravel;
            float step = Mathf.Min(remaining, Mathf.Min(Mathf.PI / 12f, cycleRemaining));
            orbitAngle += (data.clockwise ? -1f : 1f) * step;
            MoveAndHit(Center() + OrbitOffset(orbitAngle), cycle);
            orbitTravel += step; remaining -= step;
            if (orbitTravel >= Mathf.PI * 2f - 0.000001f) { orbitTravel = 0f; cycle++; }
        }
    }

    private void SelectTarget()
    {
        if (data.targetSelection == null) return;
        float range = EffectStatUtility.Safe(data.targetSelection.range, 0f, 100000f, 8f) *
            EffectExecutionScale.Safe(context.rangeMultiplier);
        targetQuery.Scan(transform.position, range, data.enemyLayerMask);
        target = data.targetSelection.Select(targetQuery.Enemies, transform.position, rangeOverride: range);
        targetLife = target != null ? target.HitEffectLifeId : 0;
    }

    private void MoveAndHit(Vector3 to, int pathCycle)
    {
        if (finished || !context.CanContinue) return;
        Vector3 from = transform.position;
        Vector3 travelDirection = to - from;
        if (travelDirection.sqrMagnitude < 0.000001f) travelDirection = direction;
        query.ScanSegment(from, to, stat.projectileHitRadius, data.enemyLayerMask);
        foreach (Enemy enemy in query.Enemies)
        {
            if (!context.CanContinue || finished) break;
            if (!query.IsCurrent(enemy) || enemy.gameObject == context.owner) continue;
            EnemyLifeKey key = new EnemyLifeKey(enemy);
            HitRecord record;
            if (hitRecords.TryGetValue(key, out record))
            {
                if (data.rehitPolicy == ProjectileRehitPolicy.OncePerLife) continue;
                if (data.rehitPolicy == ProjectileRehitPolicy.OncePerPathCycle && record.cycle == pathCycle) continue;
                if (data.rehitPolicy == ProjectileRehitPolicy.Interval && elapsed - record.time < stat.projectileRehitInterval) continue;
            }
            hitRecords[key] = new HitRecord { cycle = pathCycle, time = elapsed };
            if (!dispatcher.Hit(enemy, stat.projectileDamage, travelDirection.normalized)) continue;
            totalHits++;
            if (stat.projectileMaxHits > 0f && totalHits >= Mathf.Max(1, Mathf.RoundToInt(stat.projectileMaxHits)))
            { transform.position = enemy != null ? enemy.transform.position : from; Finish(true); break; }
        }
        if (!finished) transform.position = to;
        if (travelDirection.sqrMagnitude > 0.000001f)
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(travelDirection.y, travelDirection.x) * Mathf.Rad2Deg - 90f);
    }

    private void Finish(bool success)
    {
        if (finished) return;
        finished = true;
        if (success) CompleteLifetime();
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
