using System;
using System.Collections.Generic;
using UnityEngine;

// 다중 콜라이더 적은 한 번만 반환한다. 버퍼가 가득 차면 확장해 대상 누락을 막는다.
public sealed class EnemyQueryBuffer
{
    private Collider2D[] hits = new Collider2D[32];
    private readonly HashSet<Enemy> unique = new HashSet<Enemy>();
    private RaycastHit2D[] castHits = new RaycastHit2D[32];
    private readonly Dictionary<Enemy, float> distances = new Dictionary<Enemy, float>();
    private readonly Dictionary<Enemy, int> scannedLives = new Dictionary<Enemy, int>();
    public readonly List<Enemy> Enemies = new List<Enemy>();
    public bool IsCurrent(Enemy enemy) => enemy != null && scannedLives.TryGetValue(enemy, out int life) &&
        enemy.HitEffectLifeId == life && enemy.CanReceiveHitEffects;

    public void Scan(Vector2 center, float radius, LayerMask mask)
    {
        Enemies.Clear();
        unique.Clear();
        scannedLives.Clear();
        radius = EffectStatUtility.Safe(radius, 0f, 100000f, 0f);
        if (radius <= 0f) return;
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask);
        filter.useTriggers = true;
        int count;
        while (true)
        {
            count = Physics2D.OverlapCircle(center, radius, filter, hits);
            if (count < hits.Length) break;
            Array.Resize(ref hits, checked(hits.Length * 2));
        }
        for (int i = 0; i < count; i++)
        {
            Enemy enemy = hits[i] != null ? hits[i].GetComponentInParent<Enemy>() : null;
            hits[i] = null;
            if (enemy != null && enemy.isActiveAndEnabled && !enemy.IsDead && unique.Add(enemy))
            { Enemies.Add(enemy); scannedLives[enemy] = enemy.HitEffectLifeId; }
        }
    }

    public static Enemy Select(List<Enemy> enemies, Vector3 position, bool random)
    {
        if (enemies.Count == 0) return null;
        if (random) return enemies[UnityEngine.Random.Range(0, enemies.Count)];
        Enemy nearest = null;
        float distance = float.MaxValue;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) continue;
            float candidate = (enemy.transform.position - position).sqrMagnitude;
            if (candidate < distance) { nearest = enemy; distance = candidate; }
        }
        return nearest;
    }

    // Swept collision avoids tunneling and orders piercing hits along the path.
    public void ScanSegment(Vector2 from, Vector2 to, float radius, LayerMask mask)
    {
        Vector2 delta = to - from;
        if (delta.sqrMagnitude < 0.000001f) { Scan(from, Mathf.Max(0.001f, radius), mask); return; }
        Enemies.Clear(); unique.Clear(); distances.Clear(); scannedLives.Clear();
        ContactFilter2D filter = new ContactFilter2D();
        filter.SetLayerMask(mask); filter.useTriggers = true;
        int count;
        while (true)
        {
            count = Physics2D.CircleCast(from, Mathf.Max(0.001f, radius), delta.normalized, filter, castHits, delta.magnitude);
            if (count < castHits.Length) break;
            Array.Resize(ref castHits, checked(castHits.Length * 2));
        }
        for (int i = 0; i < count; i++)
        {
            RaycastHit2D hit = castHits[i]; castHits[i] = default(RaycastHit2D);
            Enemy enemy = hit.collider != null ? hit.collider.GetComponentInParent<Enemy>() : null;
            if (enemy == null || !enemy.CanReceiveHitEffects) continue;
            if (unique.Add(enemy)) { Enemies.Add(enemy); distances[enemy] = hit.distance; scannedLives[enemy] = enemy.HitEffectLifeId; }
            else distances[enemy] = Mathf.Min(distances[enemy], hit.distance);
        }
        Enemies.Sort((a, b) => distances[a].CompareTo(distances[b]));
    }
}
