using UnityEngine;
using UnityEngine.UI;

public class ShopBoxButton : ItemDefinitionUI
{
    private ShopManager shopManager;
    private ItemBoxData boxData;

    [Header("Shop Slot")]
    public Toggle toggle;
    public ShopSlotOpenAnimationUI openAnimation;
    public GameObject purchasedOverlay;
    public ChildrenUIControler uIControler;
    public bool IsPurchased { get; private set; }
    public ItemBoxData BoxData => boxData;
    protected override bool UpdateIcon => !IsPurchased;

    private void Awake()
    {
        if (toggle == null) toggle = GetComponent<Toggle>();
        if (toggle != null) toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnDestroy()
    {
        if (toggle != null) toggle.onValueChanged.RemoveListener(OnToggleChanged);
    }

    public void Init(ShopManager manager, ItemBoxData data)
    {
        shopManager = manager;
        boxData = data;
        IsPurchased = false;
        ClearSelection();
        if (purchasedOverlay != null) purchasedOverlay.SetActive(false);
        if (toggle != null) toggle.interactable = data != null;
        SetData(data);
        if (openAnimation != null) openAnimation.ResetToBox(data);
        if (uIControler != null) uIControler.ResetColor();
    }

    public void MarkPurchased()
    {
        if (IsPurchased) return;
        IsPurchased = true;
        if (toggle != null) toggle.interactable = false;
        if (purchasedOverlay != null) purchasedOverlay.SetActive(true);
        if (openAnimation != null) openAnimation.Play(boxData);
        if (uIControler != null) uIControler.ApplyColor();
    }

    public void ClearSelection()
    {
        if (toggle == null) return;
        var group = toggle.group;
        bool allowOff = group != null && group.allowSwitchOff;
        try
        {
            if (group != null) group.allowSwitchOff = true;
            toggle.SetIsOnWithoutNotify(false);
        }
        finally { if (group != null) group.allowSwitchOff = allowOff; }
    }

    private void OnToggleChanged(bool selected)
    {
        if (shopManager == null || boxData == null || IsPurchased)
        {
            if (selected) ClearSelection();
            return;
        }
        if (selected) shopManager.SelectBox(this, boxData);
        else if (shopManager.IsSelectedButton(this))
        {
            if (toggle.group != null && toggle.group.AnyTogglesOn()) return;
            shopManager.ClearSelectedBox();
        }
    }
}
