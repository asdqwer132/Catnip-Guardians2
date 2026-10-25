using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>배틀 씬의 수신함. 데이터만 받으며 아이템 효과/투척은 실행하지 않습니다.</summary>
public sealed class BattleItemSessionReceiver : MonoBehaviour
{
    [SerializeField] private List<InventoryItem> receivedItems = new List<InventoryItem>();
    public IReadOnlyList<InventoryItem> ReceivedItems => receivedItems;
    public event Action<GameItemToBattleData> ItemsReceived;
    private GameSession session;
    private GameSession receivedSession;
    private int receivedRevision = -1;

    private void OnEnable()
    {
        GameSession.InstanceChanged += BindSession;
        BindSession();
    }

    private void BindSession()
    {
        UnbindSession();
        session = GameSession.Instance;
        if (session != null)
        {
            if (receivedSession != session || receivedRevision != session.GameDataRevision) ResetForGame();
            session.BattleItemsAvailable += ReceivePendingItems;
            session.GameDataChanged += ResetForGame;
            ReceivePendingItems();
        }
    }

    public void ReceivePendingItems()
    {
        while (session != null && session.TryTakeBattleItems(out var data))
        {
            foreach (var item in data.items)
            {
                var existing = receivedItems.Find(entry => entry.itemData == item.itemData);
                if (existing == null) receivedItems.Add(new InventoryItem(item.itemData, item.amount));
                else existing.amount = (int)Math.Min(int.MaxValue, (long)existing.amount + item.amount);
            }
            ItemsReceived?.Invoke(data);
        }
    }

    public void ClearReceivedItems() => receivedItems.Clear();

    private void ResetForGame()
    {
        ClearReceivedItems();
        receivedSession = session;
        receivedRevision = session != null ? session.GameDataRevision : -1;
    }

    private void UnbindSession()
    {
        if (session == null) return;
        session.BattleItemsAvailable -= ReceivePendingItems;
        session.GameDataChanged -= ResetForGame;
    }

    private void OnDisable()
    {
        GameSession.InstanceChanged -= BindSession;
        UnbindSession();
        session = null;
    }
}
