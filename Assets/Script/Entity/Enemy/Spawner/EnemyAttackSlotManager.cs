using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum EnemyAttackSlotLayout { VerticalRows, RadialRings, RectangularRings }

[Serializable]
public sealed class EnemyAttackSlot
{
    public string label;
    [Tooltip("월드 좌표의 슬롯 영역입니다. 목록의 위쪽 슬롯부터 채웁니다.")]
    public Rect area = new Rect(-8f, .75f, 16f, .5f);
    [Min(0f)] public float radius = 2f;
    [Min(0f)] public float radialThickness = .5f;
    public Vector2 rectangularSize = new Vector2(4f, 4f);
    [Min(0f)] public float rectangularThickness = .5f;
    [Min(0)] public int capacity = 10;
    [SerializeField] private int reservedCount;
    public int ReservedCount => reservedCount;
    internal void SetReservedCount(int count) => reservedCount = count;
}

/// <summary>Reserves destinations before arrival, using vertical rows or concentric rings.</summary>
public sealed class EnemyAttackSlotManager : MonoBehaviour
{
    [Header("Layout")]
    public EnemyAttackSlotLayout layout;
    [Tooltip("Optional ring center. Center Offset is world-space when no transform is assigned.")]
    public Transform centerTarget;
    public Vector2 centerOffset;
    public Vector2 RingCenter => centerTarget != null ? (Vector2)centerTarget.position + centerOffset : centerOffset;

    public List<EnemyAttackSlot> slots = new List<EnemyAttackSlot>();
    [SerializeField] private int totalCapacity = -1;
    public int TotalCapacity => totalCapacity;

    // Preserve existing reservations; a shrinking band blocks new arrivals until it has room again.
    public void SetTotalCapacity(int total)
    {
        totalCapacity = Mathf.Max(0, total);
        int count = 0;
        foreach (var slot in slots) if (slot != null) count++;
        if (count == 0) return;
        int remainder = totalCapacity % count;
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            slot.capacity = totalCapacity / count + (remainder-- > 0 ? 1 : 0);
        }
    }
    [Min(.001f)] public float arrivalTolerance = .03f;
    [Tooltip("피해 없이 슬롯 공격이 완료될 때 호출됩니다. 추가 효과를 연결할 수 있습니다.")]
    public UnityEvent<Enemy> onSlotAttack = new UnityEvent<Enemy>();
    public Color gizmoColor = new Color(1f, .7f, .1f, .8f);

    private sealed class Reservation
    {
        public EnemyAttackSlot slot;
        public Vector2 point;
        public EnemyAttackSlotLayout layout;
    }
    private readonly Dictionary<Enemy, Reservation> reservations = new Dictionary<Enemy, Reservation>();
    private readonly List<Enemy> stale = new List<Enemy>();
    private int lastCleanupFrame = -1;
    private readonly Dictionary<Enemy, Vector2> sortPositions = new Dictionary<Enemy, Vector2>();
    private readonly List<Enemy> sortOrder = new List<Enemy>();
    private readonly Dictionary<Enemy, long> sortSequence = new Dictionary<Enemy, long>();
    private long nextSortSequence;

    public void SetSpawnSortPosition(Enemy enemy, Vector2 position)
    {
        if (sortPositions.ContainsKey(enemy)) return;
        sortOrder.Add(enemy);
        sortSequence.Add(enemy, nextSortSequence++);
        sortPositions.Add(enemy, position);
        RefreshSortOrder();
    }

    private void RefreshSortOrder()
    {
        sortOrder.Sort((a, b) =>
        {
            Vector2 first = sortPositions[a], second = sortPositions[b];
            int result = second.y.CompareTo(first.y);
            if (result == 0) result = second.x.CompareTo(first.x);
            return result != 0 ? result : sortSequence[a].CompareTo(sortSequence[b]);
        });
        for (int i = 0; i < sortOrder.Count; i++)
            if (sortOrder[i] != null) sortOrder[i].SetSpawnSortingOrder(this, i);
    }

    public bool TryGetDestination(Enemy enemy, out Vector2 destination)
    {
        destination = default;
        if (!isActiveAndEnabled || enemy == null || enemy.IsDead || !enemy.isActiveAndEnabled) return false;
        if (lastCleanupFrame != Time.frameCount)
        {
            RemoveStaleReservations();
            lastCleanupFrame = Time.frameCount;
        }
        Vector2 position = enemy.transform.position;
        if (reservations.TryGetValue(enemy, out var reservation))
        {
            if (reservation.layout == layout && slots.Contains(reservation.slot) &&
                (layout != EnemyAttackSlotLayout.VerticalRows
                    ? Vector2.Distance(position, RingCenter) + arrivalTolerance >= reservation.point.magnitude
                    : CoversX(reservation.slot, position.x) && reservation.point.y <= position.y + arrivalTolerance))
            {
                destination = GetReservedPoint(reservation, position);
                return true;
            }
            Release(enemy);
        }

        foreach (var slot in slots)
        {
            if (slot == null || slot.ReservedCount >= Mathf.Max(0, slot.capacity) ||
                !TryCreatePoint(slot, position, out var point)) continue;
            reservation = new Reservation { slot = slot, point = point, layout = layout };
            reservations.Add(enemy, reservation);
            slot.SetReservedCount(slot.ReservedCount + 1);
            destination = GetReservedPoint(reservation, position);
            SetSpawnSortPosition(enemy, destination);
            return true;
        }
        return false;
    }

    private Vector2 GetReservedPoint(Reservation reservation, Vector2 position)
        => reservation.layout != EnemyAttackSlotLayout.VerticalRows
            ? RingCenter + reservation.point : new Vector2(position.x, reservation.point.y);

    private bool TryCreatePoint(EnemyAttackSlot slot, Vector2 position, out Vector2 point)
    {
        point = default;
        if (layout == EnemyAttackSlotLayout.VerticalRows)
        {
            if (!CoversX(slot, position.x) || slot.area.yMin > position.y + arrivalTolerance) return false;
            point = new Vector2(position.x, Mathf.Min(position.y, UnityEngine.Random.Range(slot.area.yMin, slot.area.yMax)));
            return true;
        }

        Vector2 offset = position - RingCenter;
        float distance = offset.magnitude;
        float inner = Mathf.Max(0f, slot.radius - slot.radialThickness * .5f);
        float outer = Mathf.Max(inner, slot.radius + slot.radialThickness * .5f);
        Vector2 direction = distance > .0001f ? offset / distance : Vector2.up;
        if (layout == EnemyAttackSlotLayout.RectangularRings)
        {
            EnemyRegionGeometry.RectangleHalfSizes(slot.rectangularSize, slot.rectangularThickness, out var innerSize, out var outerSize);
            inner = EnemyRegionGeometry.RectangleRayDistance(direction, innerSize);
            outer = EnemyRegionGeometry.RectangleRayDistance(direction, outerSize);
        }
        // Keep the incoming angle and never send an enemy back to a ring it has already passed.
        if (distance + arrivalTolerance < inner) return false;
        float radius = UnityEngine.Random.Range(Mathf.Min(inner, distance), Mathf.Min(outer, distance));
        point = direction * radius;
        return true;
    }

    public bool IsAtSlot(Enemy enemy)
    {
        if (!isActiveAndEnabled || enemy == null || !reservations.TryGetValue(enemy, out var reservation)) return false;
        if (enemy.IsDead || reservation.layout != layout) return false;
        Vector2 position = enemy.transform.position;
        return (layout != EnemyAttackSlotLayout.VerticalRows || CoversX(reservation.slot, position.x)) &&
            Vector2.Distance(position, GetReservedPoint(reservation, position)) <= Mathf.Max(.001f, arrivalTolerance);
    }

    public void Release(Enemy enemy)
    {
        if (ReferenceEquals(enemy, null)) return;
        if (!reservations.TryGetValue(enemy, out var reservation)) return;
        reservations.Remove(enemy);
        reservation.slot.SetReservedCount(Mathf.Max(0, reservation.slot.ReservedCount - 1));
    }

    public void RemovePresentation(Enemy enemy)
    {
        if (!sortPositions.Remove(enemy)) return;
        sortOrder.Remove(enemy);
        sortSequence.Remove(enemy);
        RefreshSortOrder();
    }

    public void ClearReservations()
    {
        reservations.Clear();
        sortPositions.Clear();
        sortOrder.Clear();
        sortSequence.Clear();
        foreach (var slot in slots) slot?.SetReservedCount(0);
    }

    public void NotifyAttack(Enemy enemy)
    {
        if (IsAtSlot(enemy)) onSlotAttack?.Invoke(enemy);
    }

    private static bool CoversX(EnemyAttackSlot slot, float x)
        => slot != null && slot.area.width > 0f && slot.area.height >= 0f &&
            x >= slot.area.xMin && x <= slot.area.xMax;

    private void RemoveStaleReservations()
    {
        stale.Clear();
        foreach (var entry in reservations)
            if (entry.Key == null || entry.Key.IsDead || !entry.Key.isActiveAndEnabled) stale.Add(entry.Key);
        foreach (var enemy in stale) Release(enemy);
    }

    private void OnDisable() => ClearReservations();

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = gizmoColor;
        foreach (var slot in slots)
        {
            if (slot == null) continue;
            if (layout == EnemyAttackSlotLayout.VerticalRows)
                Gizmos.DrawWireCube(slot.area.center, new Vector3(slot.area.width, slot.area.height, 0f));
            else if (layout == EnemyAttackSlotLayout.RectangularRings)
            {
                EnemyRegionGeometry.RectangleHalfSizes(slot.rectangularSize, slot.rectangularThickness, out var inner, out var outer);
                Gizmos.DrawWireCube(RingCenter, inner * 2f);
                Gizmos.DrawWireCube(RingCenter, outer * 2f);
            }
            else
            {
                DrawRing(Mathf.Max(0f, slot.radius - slot.radialThickness * .5f));
                DrawRing(Mathf.Max(0f, slot.radius + slot.radialThickness * .5f));
            }
        }
    }

    private void DrawRing(float radius)
    {
        Vector3 center = RingCenter;
        Vector3 previous = center + Vector3.right * radius;
        for (int i = 1; i <= 64; i++)
        {
            float angle = i * Mathf.PI * 2f / 64;
            Vector3 next = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }
    }
}
