using UnityEngine;

public class SettingUIButton : MonoBehaviour
{
    public bool isTimeStop = false;
    public void OpenSetting()
    {
        if (SettingWindowController.instance == null)
        {
            Debug.LogWarning("SettingWindowController가 없습니다.");
            return;
        }

        SettingWindowController.instance.OpenSetting(!isTimeStop);
    }

    public void CloseSetting()
    {
        if (SettingWindowController.instance == null)
        {
            Debug.LogWarning("SettingWindowController가 없습니다.");
            return;
        }

        SettingWindowController.instance.CloseSetting();
    }
}