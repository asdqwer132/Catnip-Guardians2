using UnityEngine;
using UnityEngine.EventSystems;

public class ItemUseManager : MonoBehaviour
{
    [Header("References")]
    public GameObject owner;

    [Header("Managers")]
    public BagSelectManager bagSelectManager;
    public BagCooldownManager bagCooldownManager;
    public ItemUsePositionProvider positionProvider;
    public BagCooldownUIController cooldownUIController;

    [Header("Debug Input")]
    public bool useResetCooldownKey = true;

    private void Awake()
    {
        if (owner == null)
            owner = gameObject;

        if (bagSelectManager == null)
            bagSelectManager = GetComponent<BagSelectManager>();

        if (bagCooldownManager == null)
            bagCooldownManager = GetComponent<BagCooldownManager>();

        if (bagCooldownManager == null)
            bagCooldownManager = GetComponentInChildren<BagCooldownManager>(true);

        if (positionProvider == null)
            positionProvider = GetComponent<ItemUsePositionProvider>();

        if (cooldownUIController == null)
            cooldownUIController = GetComponent<BagCooldownUIController>();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        UnsubscribeCooldownManager();
    }

    public void Init()
    {
        if (bagSelectManager != null)
            bagSelectManager.Init();

        if (bagCooldownManager != null)
            bagCooldownManager.Init();

        if (cooldownUIController != null)
            cooldownUIController.Init(bagSelectManager);

        ResetAllCooldowns();
        RefreshSelectUI();

        SubscribeInput();
        SubscribeCooldownManager();
    }

    private void SubscribeInput()
    {
        if (GameInputManager.instance == null)
            return;

        GameInputManager.instance.OnUseItemPressed -= HandleUseInput;
        GameInputManager.instance.OnNumberPressed -= HandleBagSelectInput;
        GameInputManager.instance.OnResetAllCooldownPressed -= HandleResetCooldownInput;

        GameInputManager.instance.OnUseItemPressed += HandleUseInput;
        GameInputManager.instance.OnNumberPressed += HandleBagSelectInput;
        GameInputManager.instance.OnResetAllCooldownPressed += HandleResetCooldownInput;
    }

    private void UnsubscribeInput()
    {
        if (GameInputManager.instance == null)
            return;

        GameInputManager.instance.OnUseItemPressed -= HandleUseInput;
        GameInputManager.instance.OnNumberPressed -= HandleBagSelectInput;
        GameInputManager.instance.OnResetAllCooldownPressed -= HandleResetCooldownInput;
    }

    private void SubscribeCooldownManager()
    {
        if (bagCooldownManager == null)
            return;

        bagCooldownManager.OnAnyCooldownChanged -= HandleAnyCooldownChanged;
        bagCooldownManager.OnAnyCooldownReady -= HandleAnyCooldownReady;

        bagCooldownManager.OnAnyCooldownChanged += HandleAnyCooldownChanged;
        bagCooldownManager.OnAnyCooldownReady += HandleAnyCooldownReady;
    }

    private void UnsubscribeCooldownManager()
    {
        if (bagCooldownManager == null)
            return;

        bagCooldownManager.OnAnyCooldownChanged -= HandleAnyCooldownChanged;
        bagCooldownManager.OnAnyCooldownReady -= HandleAnyCooldownReady;
    }

    private void HandleAnyCooldownChanged()
    {
        RefreshSelectUI();
    }

    private void HandleAnyCooldownReady(BagItemUseManager manager)
    {
        RefreshSelectUI();
    }

    private void HandleBagSelectInput(int index)
    {
        if (bagSelectManager == null)
            return;

        bagSelectManager.HandleBagSelectInput();
        RefreshSelectUI();
    }

    private void HandleUseInput()
    {
        if (IsPointerOverUI())
            return;

        UseCurrentBagItem();
    }

    private void HandleResetCooldownInput()
    {
        if (!useResetCooldownKey)
            return;

        ResetAllCooldowns();
    }

    public void UseCurrentBagItem()
    {
        if (bagSelectManager == null || positionProvider == null)
            return;

        BagItemUseManager bagManager = bagSelectManager.CurrentBagUseManager;

        if (bagManager == null)
            return;

        Vector3 startPosition = positionProvider.GetUseStartPosition(owner);
        Vector3 targetPosition = positionProvider.GetMouseWorldPosition();

        bool used = bagManager.TryUseNextItem(startPosition, targetPosition, owner);

        if (used)
            RefreshSelectUI();
    }

    public void ResetAllCooldowns()
    {
        if (bagCooldownManager != null)
        {
            bagCooldownManager.ResetAllCooldowns();
            RefreshSelectUI();
            return;
        }

        if (bagSelectManager == null)
            return;

        bagSelectManager.ResetAllCooldowns();
        RefreshSelectUI();
    }

    private void RefreshSelectUI()
    {
        if (cooldownUIController == null)
            return;

        cooldownUIController.RefreshSelection();
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}
