using UnityEngine;

public class SettingUIButton : MonoBehaviour
{
    public void OpenSetting()
    {
        if (SettingManager.instance == null)
        {
            Debug.LogWarning("SettingManager가 없습니다.");
            return;
        }

        SettingManager.instance.OpenSetting();
    }

    public void CloseSetting()
    {
        if (SettingManager.instance == null)
        {
            Debug.LogWarning("SettingManager가 없습니다.");
            return;
        }

        SettingManager.instance.CloseSetting();
    }

    public void ToggleSetting()
    {
        if (SettingManager.instance == null)
        {
            Debug.LogWarning("SettingManager가 없습니다.");
            return;
        }

        SettingManager.instance.ToggleSetting();
    }
}