using UnityEngine;
using UnityEngine.UI;

public class ShopBoxButton : SlotUI
{
    public ClassificationModule classification = new ClassificationModule();
    protected override ClassificationModule Classification => classification;

    private ShopManager shopManager;
    private ItemBoxData boxData;

    [Header("Shop Slot")]
    public Toggle toggle;
    [SerializeField] private Image materialRarityImage;
    [SerializeField] private Image backgroundImage;
    private Sprite defaultBackground;
    private Sprite defaultRaritySprite;

    public void SetMaterialRarity(Sprite sprite)
    {
        if (materialRarityImage != null) materialRarityImage.sprite = sprite != null ? sprite : defaultRaritySprite;
    }
    public ShopSlotOpenAnimationUI openAnimation;
    public GameObject purchasedOverlay;
    public ChildrenUIControler uIControler;
    public bool IsPurchased { get; private set; }
    public ItemBoxData BoxData => boxData;
    protected override bool UpdateIcon => !IsPurchased;

    private void Awake()
    {
        if (backgroundImage != null) defaultBackground = backgroundImage.sprite;
        if (materialRarityImage != null) defaultRaritySprite = materialRarityImage.sprite;
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
        if (backgroundImage != null)
        {
            backgroundImage.sprite = data != null && data.background != null ? data.background : defaultBackground;
            backgroundImage.enabled = backgroundImage.sprite != null;
        }
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
