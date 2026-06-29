using UnityEngine;

public class SettingWindowController : MonoBehaviour
{
    public static SettingWindowController instance;

    [Header("Panel")]
    public GameObject settingPanel;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void OpenSetting(bool isTimeStop)
    {
        if (settingPanel == null)
            return;
        if(isTimeStop)
            Time.timeScale = 0f;
        settingPanel.SetActive(true);
    }

    public void CloseSetting(bool isTimeStop)
    {
        if (settingPanel == null)
            return;
        if (isTimeStop)
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