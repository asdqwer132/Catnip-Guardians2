using System;
using System.Collections.Generic;
using UnityEngine;

// 다중 콜라이더 적은 한 번만 반환한다. 버퍼가 가득 차면 확장해 대상 누락을 막는다.
public sealed class EnemyQueryBuffer
{
    private Collider2D[] hits = new Collider2D[32];
    private readonly HashSet<Enemy> unique = new HashSet<Enemy>();
    public readonly List<Enemy> Enemies = new List<Enemy>();

    public void Scan(Vector2 center, float radius, LayerMask mask)
    {
        Enemies.Clear();
        unique.Clear();
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
                Enemies.Add(enemy);
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
}
