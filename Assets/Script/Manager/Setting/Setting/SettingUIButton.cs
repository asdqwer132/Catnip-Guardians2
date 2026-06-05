using UnityEngine;

public class SettingUIButton : MonoBehaviour
{
    public void OpenSetting()
    {
        if (SettingWindowController.instance == null)
        {
            Debug.LogWarning("SettingWindowController가 없습니다.");
            return;
        }

        SettingWindowController.instance.OpenSetting();
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