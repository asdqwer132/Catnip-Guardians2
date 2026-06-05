using UnityEngine;

public class SettingWindowController : MonoBehaviour
{
    public static SettingWindowController instance;

    [Header("Panel")]
    public GameObject settingPanel;

    private void Awake()
    {
        instance = this;
    }

    public void OpenSetting()
    {
        if (settingPanel == null)
            return;

        Time.timeScale = 0f;
        settingPanel.SetActive(true);
    }

    public void CloseSetting()
    {
        if (settingPanel == null)
            return;
        Time.timeScale = 1.0f;
        settingPanel.SetActive(false);
    }

    public void ToggleSetting()
    {
        if (settingPanel == null)
            return;

        settingPanel.SetActive(!settingPanel.activeSelf);
    }
}