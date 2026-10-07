using System;
using UnityEngine;

// 자체 탄환: 아이템을 재사용하거나 버프 사용 횟수를 추가로 소비하지 않는다.
public sealed class SummonProjectile : MonoBehaviour
{
    private ItemEffectContext context;
    private ItemEffectLease lease;
    private Enemy target;
    private int targetLife;
    private float damage, speed, remaining, radius;
    private bool homing, initialized, finished;
    private Vector2 direction;
    private ContactFilter2D filter;
    private HitEffectData[] effects;
    private RaycastHit2D[] hits = new RaycastHit2D[16];

    public void Init(ItemEffectContext context, Enemy target, float damage, float speed, float lifetime,
        float radius, bool homing, LayerMask mask, HitEffectData[] effects, Sprite sprite)
    {
        this.context = context;
        this.target = target;
        targetLife = target != null ? target.HitEffectLifeId : 0;
        this.damage = damage;
        this.speed = speed;
        remaining = lifetime;
        this.radius = radius;
        this.homing = homing;
        this.effects = effects;
        lease = context.RetainLifetime();
        direction = context.direction;
        if (direction.sqrMagnitude < 0.000001f) direction = Vector2.right;
        direction.Normalize();
        filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = true;
        if (sprite != null)
        {
            SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
        }
        initialized = true;
    }

    private void Update()
    {
        if (!initialized || finished) return;
        if (!context.CanContinue) { Finish(false); return; }
        float travelTime = Mathf.Min(remaining, Time.deltaTime);
        remaining -= Time.deltaTime;
        if (homing && target != null && target.HitEffectLifeId == targetLife && !target.IsDead && target.isActiveAndEnabled)
        {
            Vector2 toTarget = target.transform.position - transform.position;
            if (toTarget.sqrMagnitude > 0.000001f) direction = toTarget.normalized;
        }
        Vector2 position = transform.position;
        float distance = speed * Mathf.Max(0f, travelTime);
        int count;
        while (true)
        {
            count = Physics2D.CircleCast(position, radius, direction, filter, hits, distance);
            if (count < hits.Length) break;
            Array.Resize(ref hits, checked(hits.Length * 2));
        }
        Enemy enemy = null;
        float nearest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = hits[i];
            hits[i] = default(RaycastHit2D);
            Enemy candidate = hit.collider != null ? hit.collider.GetComponentInParent<Enemy>() : null;
            if (candidate == null || candidate.IsDead || !candidate.isActiveAndEnabled || hit.distance >= nearest) continue;
            nearest = hit.distance;
            enemy = candidate;
        }
        if (enemy != null)
        {
            transform.position = position + direction * nearest;
            try { SummonDamageUtility.Hit(enemy, damage, effects, context.Copy(enemy.transform.position, direction)); }
            finally { Finish(true); }
            return;
        }
        transform.position = position + direction * distance;
        if (remaining <= 0f) Finish(true);
    }

    private void Finish(bool succeeded)
    {
        if (finished) return;
        finished = true;
        if (lease != null) lease.Finish(succeeded);
        lease = null;
        Destroy(gameObject);
    }
    private void OnDisable() => Finish(false);
}
