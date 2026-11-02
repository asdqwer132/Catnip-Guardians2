using UnityEngine;
using UnityEngine.UI;

public class ShopBoxButton : MonoBehaviour
{
    private ShopManager shopManager;
    private ItemBoxData boxData;


    [Header("UI")]
    public Image icon;
    public Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();
    }

    private void Start()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(isOn =>
            {
                if (isOn)
                    OnClickButton();
            });
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(_ => OnClickButton());
    }
    public void Init(ShopManager manager, ItemBoxData data)
    {
        shopManager = manager;
        boxData = data;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (boxData == null)
        {
            ClearUI();
            return;
        }

        if (icon != null)
        {
            icon.sprite = boxData.icon;
            icon.enabled = boxData.icon != null;
        }
    }

    private void ClearUI()
    {
        if (icon != null)
        {
            icon.sprite = null;
            icon.enabled = false;
        }
    }

    private void OnClickButton()
    {
        if (boxData == null)
        {
            Debug.LogWarning("버튼에 연결된 상자 데이터가 없습니다.");
            return;
        }

        if (shopManager == null)
        {
            Debug.LogWarning("ShopManager가 연결되지 않았습니다.");
            return;
        }

        if (shopManager.IsSelectedButton(this))
        {
            shopManager.ClearSelectedBox();
            return;
        }

        shopManager.SelectBox(this, boxData);
    }
}