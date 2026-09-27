using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftUIManager : MonoBehaviour
{
    // Inventory slots instantiated from prefabs can use the currently open craft screen.
    public static CraftUIManager Active { get; private set; }

    [Header("Reffrence - Crafting")]
    [SerializeField] private ItemCraftManager itemCraftManager;
    public CraftMaterialSlotUI[] slots;

    [Header("Optional result effect")]
    [SerializeField] private CraftSuccessEffectUI craftSuccessEffectUI;
    [SerializeField, Min(0.05f)] private float secondsBetweenResults = 1f;

    private ItemCraftManager subscribedManager;
    private GeneralCraftManager subscribedGeneral;
    private readonly Queue<ItemData> pendingEffects = new Queue<ItemData>();
    private Coroutine effectRoutine;

    private void OnEnable()
    {
        Active = this;
        ConnectGeneralManager();
        ResolveManager();
        RefreshUI();
    }

    // Also resolves the singleton after all scene objects have run Awake.
    private void Start()
    {
        ConnectGeneralManager();
        ResolveManager();
        RefreshUI();
    }

    private void OnDisable()
    {
        if (Active == this)
            Active = null;

        if (subscribedGeneral != null)
        {
            subscribedGeneral.CurrentManagerChanged -= OnManagerChanged;
            subscribedGeneral = null;
        }

        if (subscribedManager != null)
        {
            subscribedManager.onMaterialChanged -= RefreshUI;
            subscribedManager.CraftCompleted -= ShowCraftResult;
            subscribedManager = null;
        }

        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        pendingEffects.Clear();
    }

    private void ConnectGeneralManager()
    {
        GeneralCraftManager general = GeneralCraftManager.instance;
        if (subscribedGeneral != general)
        {
            if (subscribedGeneral != null)
                subscribedGeneral.CurrentManagerChanged -= OnManagerChanged;

            subscribedGeneral = general;
            if (subscribedGeneral != null)
                subscribedGeneral.CurrentManagerChanged += OnManagerChanged;
        }

        if (general != null && general.GetCurrentManager() != null)
            itemCraftManager = general.GetCurrentManager();
    }

    private void OnManagerChanged(ItemCraftManager manager)
    {
        itemCraftManager = manager;
        if (effectRoutine != null)
        {
            StopCoroutine(effectRoutine);
            effectRoutine = null;
        }

        pendingEffects.Clear();
        RefreshUI();
    }

    private ItemCraftManager ResolveManager()
    {
        if (itemCraftManager == null)
            itemCraftManager = ItemCraftManager.instance;

        if (isActiveAndEnabled && subscribedManager != itemCraftManager)
        {
            if (subscribedManager != null)
            {
                subscribedManager.onMaterialChanged -= RefreshUI;
                subscribedManager.CraftCompleted -= ShowCraftResult;
            }

            subscribedManager = itemCraftManager;
            if (subscribedManager != null)
            {
                subscribedManager.onMaterialChanged += RefreshUI;
                subscribedManager.CraftCompleted += ShowCraftResult;
            }
        }

        return itemCraftManager;
    }

    public ItemCraftManager Manager => ResolveManager();

    public void RefreshUI()
    {
        ItemCraftManager manager = ResolveManager();
        if (slots == null)
            return;

        IReadOnlyList<InventoryItem> materials = manager != null ? manager.CurrentMaterials : null;
        for (int i = 0; i < slots.Length; i++)
        {
            CraftMaterialSlotUI slot = slots[i];
            if (slot == null)
                continue;

            slot.Bind(manager);
            if (materials != null && i < materials.Count && materials[i] != null)
            {
                slot.SetSlot(materials[i]);
                slot.SetAmount(materials[i].amount);
                if (slot.Pannel != null)
                    slot.Pannel.SetActive(true);
            }
            else
            {
                slot.ClearSlot();
                slot.SetAmount(0);
                if (slot.Pannel != null)
                    slot.Pannel.SetActive(false);
            }
        }
    }

    // These can be wired to the inventory click, craft button, and cancel button.
    public void AddMaterial(ItemData item)
    {
        ItemCraftManager manager = ResolveManager();
        if (manager != null)
            manager.AddMaterial(item);
    }

    public void Combine()
    {
        ItemCraftManager manager = ResolveManager();
        if (manager != null)
            manager.Combine();
    }

    public void ReturnMaterials()
    {
        ItemCraftManager manager = ResolveManager();
        if (manager != null)
            manager.ReturnMaterials();
    }

    private void ShowCraftResult(CraftResult result)
    {
        if (craftSuccessEffectUI == null || result == null)
            return;

        foreach (ItemData item in result.CraftedItems)
        {
            if (item != null)
                pendingEffects.Enqueue(item);
        }

        if (result.FailureItem != null)
            pendingEffects.Enqueue(result.FailureItem);

        if (effectRoutine == null && pendingEffects.Count > 0)
            effectRoutine = StartCoroutine(PlayResultEffects());
    }

    private IEnumerator PlayResultEffects()
    {
        while (pendingEffects.Count > 0)
        {
            ItemData item = pendingEffects.Dequeue();
            craftSuccessEffectUI.Play(item.icon);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, secondsBetweenResults));
        }

        effectRoutine = null;
    }
}
