using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class ItemUseManager : MonoBehaviour
{
    [Header("References")]
    public GameObject owner;

    [Header("Managers")]
    public BagSelectManager bagSelectManager;
    public ItemUsePositionProvider positionProvider;
    public BagCooldownUIController cooldownUIController;

    [Header("Debug Input")]
    public bool useResetCooldownKey = true;

    [Tooltip("»õ Input System¿ë Å°")]
    public Key resetAllCooldownKey = Key.R;

    private void Awake()
    {
        if (owner == null)
            owner = gameObject;

        if (bagSelectManager == null)
            bagSelectManager = GetComponent<BagSelectManager>();

        if (positionProvider == null)
            positionProvider = GetComponent<ItemUsePositionProvider>();

        if (cooldownUIController == null)
            cooldownUIController = GetComponent<BagCooldownUIController>();
    }

    private void Update()
    {
        HandleBagSelectInput();
        HandleUseInput();
        HandleResetCooldownInput();
    }

    public void Init()
    {
        if (bagSelectManager != null)
            bagSelectManager.Init();

        if (cooldownUIController != null)
            cooldownUIController.Init(bagSelectManager);

        ResetAllCooldowns();
        RefreshSelectUI();
    }

    private void HandleBagSelectInput()
    {
        if (bagSelectManager == null)
            return;

        bagSelectManager.HandleBagSelectInput();
    }

    private void HandleUseInput()
    {
        if (Mouse.current == null)
            return;

        if (!Mouse.current.leftButton.wasPressedThisFrame)
            return;

        if (IsPointerOverUI())
            return;

        UseCurrentBagItem();
    }

    private void HandleResetCooldownInput()
    {
        if (!useResetCooldownKey)
            return;

        if (Keyboard.current == null)
            return;

        if (!Keyboard.current[resetAllCooldownKey].wasPressedThisFrame)
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