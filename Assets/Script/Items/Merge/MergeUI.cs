using TMPro;
using UnityEngine;

public class MergeUI : MonoBehaviour
{
    public TextMeshProUGUI countText;
    public TextMeshProUGUI defaultCountText;
    public GameObject slotObj;

    public void SetSlot(bool isActive) => slotObj.SetActive(isActive);
    public void SetCount(int c, int dc)
    {

        if (countText != null)
            countText.text = "" + c;
        if (defaultCountText != null)
            defaultCountText.text = "(+" + dc + ")";
    }
}
