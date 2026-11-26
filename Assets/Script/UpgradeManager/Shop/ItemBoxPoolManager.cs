using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemBoxPoolManager : MonoBehaviour
{
    [Header("All Box List")]
    public ItemBoxData[] allBoxList;

    [Header("Series Selection")]
    [Tooltip("Weapon, Food, Mineral, Monster, Fishing, Plant, Machine, Magic 순서. 배열이 비어 있으면 전체 사용. 연결된 토글이 모두 꺼져 있으면 추첨하지 않습니다.")]
    public Toggle[] seriesToggles;

    [Header("Option")]
    public bool allowDuplicate = false;

    private readonly Dictionary<ItemSeries, List<ItemBoxData>> boxesBySeries = new Dictionary<ItemSeries, List<ItemBoxData>>();
    private readonly List<ItemBoxData> cachedBoxes = new List<ItemBoxData>();
    private bool cacheReady;

    private void Awake() => RebuildCache();
    private void OnValidate() => cacheReady = false;

    /// <summary>Call after changing the box list or a box's series at runtime.</summary>
    public void RebuildCache()
    {
        boxesBySeries.Clear();
        cachedBoxes.Clear();
        var seen = new HashSet<ItemBoxData>();
        if (allBoxList != null)
        {
            foreach (var box in allBoxList)
            {
                if (box == null || !seen.Add(box)) continue;
                cachedBoxes.Add(box);
                if (!boxesBySeries.TryGetValue(box.series, out var seriesBoxes))
                {
                    seriesBoxes = new List<ItemBoxData>();
                    boxesBySeries.Add(box.series, seriesBoxes);
                }
                seriesBoxes.Add(box);
            }
        }
        cacheReady = true;
    }

    public List<ItemBoxData> GetRandomBoxes(int count, ShopBoxKind kind = ShopBoxKind.Material)
    {
        var result = new List<ItemBoxData>();
        if (count <= 0) return result;
        if (!cacheReady) RebuildCache();

        var availableBoxes = new List<ItemBoxData>();
        if (kind == ShopBoxKind.Pin || seriesToggles == null || seriesToggles.Length == 0)
            availableBoxes.AddRange(cachedBoxes);
        else
        {
            // ItemSeries Weapon starts at 1; index 0 is the Weapon toggle.
            for (int i = 0; i < seriesToggles.Length; i++)
            {
                if (seriesToggles[i] != null && seriesToggles[i].isOn &&
                    boxesBySeries.TryGetValue((ItemSeries)(i + 1), out var seriesBoxes))
                    availableBoxes.AddRange(seriesBoxes);
            }
        }

        availableBoxes.RemoveAll(box => box.kind != kind);

        for (int i = 0; i < count && availableBoxes.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableBoxes.Count);
            result.Add(availableBoxes[randomIndex]);
            if (!allowDuplicate) availableBoxes.RemoveAt(randomIndex);
        }
        return result;
    }
}
