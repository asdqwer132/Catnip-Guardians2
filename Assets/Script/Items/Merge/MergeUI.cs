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
        // Exhausted material stays visible with a zero count.
        if (slotObj != null) slotObj.SetActive(true);
        if (toggle != null)
        {
            toggle.interactable = isActive;
            if (!isActive) toggle.SetIsOnWithoutNotify(false);
        }
    }
    public void SetCount(int c, int dc)
    {

        if (countText != null)
            countText.text = Mathf.Max(0, c).ToString();
        if (defaultCountText != null)
            defaultCountText.text = "(+" + Mathf.Max(0, dc) + ")";
    }
}
