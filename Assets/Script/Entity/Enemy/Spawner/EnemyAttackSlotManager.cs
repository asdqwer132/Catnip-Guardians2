using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public sealed class EnemyAttackSlot
{
    public string label;
    [Tooltip("월드 좌표의 슬롯 영역입니다. 목록의 위쪽 슬롯부터 채웁니다.")]
    public Rect area = new Rect(-8f, .75f, 16f, .5f);
    [Min(1)] public int capacity = 10;
    [SerializeField] private int reservedCount;
    public int ReservedCount => reservedCount;
    internal void SetReservedCount(int count) => reservedCount = count;
}

/// <summary>이동 중인 적을 포함해 자리를 예약합니다. 적마다 X를 유지하고 배정된 슬롯의 Y까지 내려옵니다.</summary>
public sealed class EnemyAttackSlotManager : MonoBehaviour
{
    public List<EnemyAttackSlot> slots = new List<EnemyAttackSlot>();
    [Min(.001f)] public float arrivalTolerance = .03f;
    [Tooltip("피해 없이 슬롯 공격이 완료될 때 호출됩니다. 추가 효과를 연결할 수 있습니다.")]
    public UnityEvent<Enemy> onSlotAttack = new UnityEvent<Enemy>();
    public Color gizmoColor = new Color(1f, .7f, .1f, .8f);

    private sealed class Reservation
    {
        public EnemyAttackSlot slot;
        public float y;
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
        float x = enemy.transform.position.x;
        if (reservations.TryGetValue(enemy, out var reservation))
        {
            if (slots.Contains(reservation.slot) && CoversX(reservation.slot, x) &&
                reservation.y <= enemy.transform.position.y + arrivalTolerance)
            {
                destination = new Vector2(x, reservation.y);
                return true;
            }
            Release(enemy);
        }

        foreach (var slot in slots)
        {
            if (!CoversX(slot, x) || slot.ReservedCount >= Mathf.Max(1, slot.capacity)) continue;
            // 이미 지나친 슬롯으로 거슬러 올라가지 않습니다.
            if (slot.area.yMin > enemy.transform.position.y + arrivalTolerance) continue;
            reservation = new Reservation { slot = slot, y = Mathf.Min(enemy.transform.position.y,
                UnityEngine.Random.Range(slot.area.yMin, slot.area.yMax)) };
            reservations.Add(enemy, reservation);
            slot.SetReservedCount(slot.ReservedCount + 1);
            destination = new Vector2(x, reservation.y);
            SetSpawnSortPosition(enemy, destination);
            return true;
        }
        return false;
    }

    public bool IsAtSlot(Enemy enemy)
    {
        if (!isActiveAndEnabled || enemy == null || !reservations.TryGetValue(enemy, out var reservation)) return false;
        return !enemy.IsDead && CoversX(reservation.slot, enemy.transform.position.x) &&
            Mathf.Abs(enemy.transform.position.y - reservation.y) <= Mathf.Max(.001f, arrivalTolerance);
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
            if (slot != null) Gizmos.DrawWireCube(slot.area.center, new Vector3(slot.area.width, slot.area.height, 0f));
    }
}
