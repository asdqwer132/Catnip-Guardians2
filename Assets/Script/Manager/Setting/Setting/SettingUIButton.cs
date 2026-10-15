using UnityEngine;

public class SettingUIButton : MonoBehaviour
{
    public bool isTimeStop = false;
    public void ResetTutorialProgress()
    {
        if (TutorialProgressManager.instance != null) TutorialProgressManager.instance.ResetProgress();
        else TutorialSave.ResetProgress();
    }
    public void ResetStoryProgress() => StorySave.ResetStoryProgress();
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
