using System.Collections.Generic;
using UnityEngine;

public class ItemBoxPoolManager : MonoBehaviour
{
    [Header("All Box List")]
    public ItemBoxData[] allBoxList;

    [Header("Option")]
    public bool allowDuplicate = false;

    public List<ItemBoxData> GetRandomBoxes(int count)
    {
        List<ItemBoxData> result = new List<ItemBoxData>();

        if (allBoxList == null || allBoxList.Length == 0)
        {
            Debug.LogWarning("전체 상자 리스트가 비어있습니다.");
            return result;
        }

        List<ItemBoxData> availableBoxes = new List<ItemBoxData>();

        for (int i = 0; i < allBoxList.Length; i++)
        {
            if (allBoxList[i] != null)
                availableBoxes.Add(allBoxList[i]);
        }

        if (availableBoxes.Count == 0)
        {
            Debug.LogWarning("사용 가능한 상자가 없습니다.");
            return result;
        }

        for (int i = 0; i < count; i++)
        {
            if (!allowDuplicate && availableBoxes.Count == 0)
                break;

            int randomIndex = Random.Range(0, availableBoxes.Count);
            ItemBoxData selectedBox = availableBoxes[randomIndex];

            result.Add(selectedBox);

            if (!allowDuplicate)
                availableBoxes.RemoveAt(randomIndex);
        }

        return result;
    }
}