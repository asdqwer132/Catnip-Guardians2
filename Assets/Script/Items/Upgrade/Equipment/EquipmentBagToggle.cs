using UnityEngine;
using UnityEngine.UI;

public class EquipmentBagToggle : MonoBehaviour
{
    public int bagIndex;
    public Toggle toggle;

    private void Awake()
    {
        if (toggle == null)
            toggle = GetComponent<Toggle>();

        toggle.onValueChanged.AddListener(OnToggleChanged);
    }

    private void OnToggleChanged(bool isOn)
    {
        if (!isOn)
            return;

        EquipmentBagManager.instance.SelectBag(bagIndex);
    }
}