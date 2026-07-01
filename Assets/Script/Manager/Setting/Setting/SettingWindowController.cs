using UnityEngine;

public class SettingWindowController : MonoBehaviour
{
    public static SettingWindowController instance;

    [Header("Panel")]
    public GameObject settingPanel;

    private bool isTimeStoped = false;

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
        isTimeStoped = isTimeStop;
        settingPanel.SetActive(true);
    }

    public void CloseSetting()
    {
        if (settingPanel == null)
            return;

        if (isTimeStoped)
            Time.timeScale = 1f;
        isTimeStoped = false ;
        settingPanel.SetActive(false);
    }

    public void ToggleSetting()
    {
        if (settingPanel == null)
            return;

        settingPanel.SetActive(!settingPanel.activeSelf);
    }
}