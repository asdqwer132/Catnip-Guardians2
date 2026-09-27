using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Shows possible craft results for the materials currently in ItemCraftManager.</summary>
public sealed class ItemCraftPredictionManager : MonoBehaviour
{
    public sealed class Prediction
    {
        public ItemData ResultItem { get; private set; }
        public float RawChancePercent { get; private set; }
        public float EffectiveChancePercent => Mathf.Min(100f, RawChancePercent);

        internal int Order { get; private set; }

        internal Prediction(ItemData resultItem, float rawChancePercent, int order)
        {
            ResultItem = resultItem;
            RawChancePercent = rawChancePercent;
            Order = order;
        }
    }

    [Header("Reffrence - Crafting")]
    [SerializeField] public ItemCraftManager craftManager;

    [Header("Prediction UI")]
    [SerializeField] private BaseItemSlotUI[] predictionSlots = new BaseItemSlotUI[0];
    [SerializeField] private bool hideEmptySlots = true;

    private readonly List<Prediction> predictions = new List<Prediction>();
    private ItemCraftManager subscribedManager;

    public IReadOnlyList<Prediction> Predictions => predictions;
    public event Action PredictionsChanged;

    private void OnEnable()
    {
        RefreshPredictions();
    }

    // Resolves the singleton even if ItemCraftManager.Awake ran after our OnEnable.
    private void Start()
    {
        RefreshPredictions();
    }

    private void OnDisable()
    {
        if (subscribedManager != null)
        {
            subscribedManager.onMaterialChanged -= RefreshPredictions;
            subscribedManager = null;
        }
    }

    private ItemCraftManager ResolveManager()
    {
        if (craftManager == null && GeneralCraftManager.instance != null)
            craftManager = GeneralCraftManager.instance.GetCurrentManager();

        if (isActiveAndEnabled && subscribedManager != craftManager)
        {
            if (subscribedManager != null)
                subscribedManager.onMaterialChanged -= RefreshPredictions;

            subscribedManager = craftManager;
            if (subscribedManager != null)
                subscribedManager.onMaterialChanged += RefreshPredictions;
        }

        return craftManager;
    }

    public void SetCraftManager(ItemCraftManager manager)
    {
        craftManager = manager;
        RefreshPredictions();
    }

    public void RefreshPredictions()
    {
        ItemCraftManager manager = ResolveManager();
        predictions.Clear();

        if (manager != null)
        {
            // Every result uses the same material snapshot, just like one craft attempt.
            CraftContext context = manager.CreateContext();
            if (context.TotalMaterialCount > 0 && manager.CraftableItems != null)
            {
                HashSet<ItemData> seenResults = new HashSet<ItemData>();
                int order = 0;

                foreach (CraftItemData candidate in manager.CraftableItems)
                {
                    if (candidate == null || candidate.resultItem == null || !seenResults.Add(candidate.resultItem))
                    {
                        order++;
                        continue;
                    }

                    float chance = candidate.GetRawChancePercent(context);
                    if (!float.IsNaN(chance) && chance > 0f)
                        predictions.Add(new Prediction(candidate.resultItem, chance, order));

                    order++;
                }

                predictions.Sort((a, b) =>
                {
                    int effectiveOrder = b.EffectiveChancePercent.CompareTo(a.EffectiveChancePercent);
                    if (effectiveOrder != 0)
                        return effectiveOrder;

                    int rawOrder = b.RawChancePercent.CompareTo(a.RawChancePercent);
                    return rawOrder != 0 ? rawOrder : a.Order.CompareTo(b.Order);
                });
            }
        }

        RefreshSlots();
        PredictionsChanged?.Invoke();
    }

    private void RefreshSlots()
    {
        int slotCount = predictionSlots != null ? predictionSlots.Length : 0;
        for (int i = 0; i < slotCount; i++)
        {
            BaseItemSlotUI slot = predictionSlots[i];
            if (slot == null)
                continue;

            bool hasPrediction = i < predictions.Count;
            // SetItemData does not clear currentItem, so clear it before reuse.
            slot.ClearSlot();
            if (hasPrediction)
            {
                slot.SetItemData(predictions[i].ResultItem);
                // The base slot's amount field is the effective craft probability here.
                if (slot.amountText != null)
                    slot.amountText.text = predictions[i].EffectiveChancePercent.ToString("0.###") + "%";
            }

            if (hideEmptySlots && slot.gameObject != gameObject)
                slot.gameObject.SetActive(hasPrediction);
        }
    }
}
