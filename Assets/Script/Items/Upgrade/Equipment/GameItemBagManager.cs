using System.Collections.Generic;
using UnityEngine;

public class GameItemBagManager : MonoBehaviour
{
    [Header("Game Scene Bags")]
    // GameScene에 실제로 존재하는 EquipmentBag들을
    // Inspector에서 순서대로 연결
    public List<EquipmentBag> bags = new List<EquipmentBag>();

    [Header("Bag Item Use Managers")]
    public BagItemUseManager[] bagItemUseManagers;

    public void Init()
    {
        if (bags == null || bags.Count == 0)
        {
            Debug.LogWarning("GameItemBagManager : GameScene의 EquipmentBag이 없습니다.");
            return;
        }

        // 1. GameScene의 EquipmentBag 자체를 먼저 초기화
        foreach (EquipmentBag bag in bags)
        {
            if (bag != null)
                bag.Init();
        }

        // 2. GameSession에 저장된 데이터를
        //    GameScene의 EquipmentBag 객체에 복원
        if (GameSession.Instance != null &&
            GameSession.Instance.HasEquipmentBagData)
        {
            GameSession.Instance.LoadEquipmentBags(bags);
        }

        // 3. 실제 아이템 사용 매니저에
        //    GameScene의 EquipmentBag 연결
        ConnectBagItemUseManagers();
    }

    private void ConnectBagItemUseManagers()
    {
        if (bagItemUseManagers == null)
            return;

        int count = Mathf.Min(
            bags.Count,
            bagItemUseManagers.Length
        );

        for (int i = 0; i < count; i++)
        {
            if (bagItemUseManagers[i] == null)
                continue;

            bagItemUseManagers[i].bag = bags[i];
        }

        if (bags.Count != bagItemUseManagers.Length)
        {
            Debug.LogWarning(
                $"가방 개수({bags.Count})와 " +
                $"BagItemUseManager 개수({bagItemUseManagers.Length})가 다릅니다."
            );
        }
    }

    public EquipmentBag GetBagData(string bagId)
    {
        if (string.IsNullOrEmpty(bagId))
            return null;

        if (bags == null)
            return null;

        for (int i = 0; i < bags.Count; i++)
        {
            EquipmentBag bag = bags[i];

            if (bag == null)
                continue;

            BagData bagData = bag.bagData;

            if (bagData == null)
                continue;

            if (bagData.dataId == bagId)
                return bag;
        }

        return null;
    }
}