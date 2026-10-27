using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MergeUI : MonoBehaviour
{
    public TextMeshProUGUI countText;
    public TextMeshProUGUI defaultCountText;
    public GameObject slotObj;
    public Toggle toggle;
    public void SetSlot(bool isActive)
    {
        if (slotObj != null) slotObj.SetActive(isActive);
    }
    public void SetCount(int c, int dc)
    {

        if (countText != null)
            countText.text = "" + c;
        if (defaultCountText != null)
            defaultCountText.text = "(+" + dc + ")";
    }
}
