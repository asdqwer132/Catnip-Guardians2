using UnityEngine;
using UnityEngine.EventSystems;

public class ItemUseManager : MonoBehaviour
{
    [Header("References")]
    public GameObject owner;

    [Header("Managers")]
    public BagSelectManager bagSelectManager;

    [Header("Helpers")]
    public ItemUsePositionProvider positionProvider;
    public BagCooldownUIController cooldownUIController;

    [Header("Debug Input")]
    public bool useResetCooldownKey = true;
    public KeyCode resetAllCooldownKey = KeyCode.R;

    void Awake()
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

    public void Init()
    {
        if (bagSelectManager != null)
            bagSelectManager.Init();

        if (cooldownUIController != null)
            cooldownUIController.Init(bagSelectManager);
        ResetAllCooldowns();
        UpdateCooldownUI();
    }

    void Update()
    {
        HandleBagSelectInput();
        HandleUseInput();
        HandleResetCooldownInput();
        UpdateCooldownUI();
    }

    private void HandleBagSelectInput()
    {
        if (bagSelectManager == null)
            return;

        bagSelectManager.HandleBagSelectInput();
    }

    private void HandleUseInput()
    {
        if (!Input.GetMouseButtonDown(0))
            return;

        if (IsPointerOverUI())
            return;

        UseCurrentBagItem();
    }

    private void HandleResetCooldownInput()
    {
        if (!useResetCooldownKey)
            return;

        if (!Input.GetKeyDown(resetAllCooldownKey))
            return;

        ResetAllCooldowns();
    }

    public void UseCurrentBagItem()
    {
        if (bagSelectManager == null)
        {
            Debug.LogWarning("BagSelectManager가 없습니다.");
            return;
        }

        if (positionProvider == null)
        {
            Debug.LogWarning("ItemUsePositionProvider가 없습니다.");
            return;
        }

        BagItemUseManager bagManager = bagSelectManager.CurrentBagUseManager;

        if (bagManager == null)
        {
            Debug.LogWarning("현재 선택된 가방 매니저가 없습니다.");
            return;
        }

        Vector3 startPosition = positionProvider.GetUseStartPosition(owner);
        Vector3 targetPosition = positionProvider.GetMouseWorldPosition();

        bagManager.TryUseNextItem(startPosition, targetPosition, owner);
    }

    public void SelectBag(int index)
    {
        if (bagSelectManager == null)
        {
            Debug.LogWarning("BagSelectManager가 없습니다.");
            return;
        }

        bool success = bagSelectManager.SelectBag(index);

        if (success)
        {
            UpdateCooldownUI();
        }
    }

    public void RebuildAllBagSlotUIs()
    {
        if (cooldownUIController == null)
            return;

        cooldownUIController.Init(bagSelectManager);
        UpdateCooldownUI();
    }

    public void ResetCurrentBagUsePosition()
    {
        if (bagSelectManager == null)
            return;

        bagSelectManager.ResetCurrentBagUsePosition();
        UpdateCooldownUI();
    }

    public void ResetAllBagUsePositions()
    {
        if (bagSelectManager == null)
            return;

        bagSelectManager.ResetAllBagUsePositions();
        UpdateCooldownUI();
    }

    public void ResetAllCooldowns()
    {
        if (bagSelectManager == null)
        {
            Debug.LogWarning("BagSelectManager가 없어서 전체 쿨타임을 초기화할 수 없습니다.");
            return;
        }

        bagSelectManager.ResetAllCooldowns();

        UpdateCooldownUI();

        Debug.Log("전체 가방 쿨타임과 아이템 준비시간을 초기화했습니다.");
    }

    public void ResetCurrentBagCooldowns()
    {
        if (bagSelectManager == null)
        {
            Debug.LogWarning("BagSelectManager가 없어서 현재 가방 쿨타임을 초기화할 수 없습니다.");
            return;
        }

        BagItemUseManager currentBagManager = bagSelectManager.CurrentBagUseManager;

        if (currentBagManager == null)
        {
            Debug.LogWarning("현재 선택된 가방 매니저가 없습니다.");
            return;
        }

        currentBagManager.ResetAllCooldowns();

        UpdateCooldownUI();

        Debug.Log("현재 가방의 쿨타임과 아이템 준비시간을 초기화했습니다.");
    }

    private void UpdateCooldownUI()
    {
        if (cooldownUIController == null)
            return;

        cooldownUIController.UpdateUI(bagSelectManager);
    }

    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        return EventSystem.current.IsPointerOverGameObject();
    }
}