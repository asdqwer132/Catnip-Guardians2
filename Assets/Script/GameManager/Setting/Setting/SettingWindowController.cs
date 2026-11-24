using UnityEngine;

public class SettingWindowController : MonoBehaviour
{
    public static SettingWindowController instance;
    public GameObject settingPanel;
    public bool pauseWhenToggled = true;
    public bool IsOpen => settingPanel != null && settingPanel.activeSelf;
    private GameInputManager input;
    private void Awake()
    {
        if (instance != null && instance != this) { enabled = false; return; }
        instance = this;
        if (settingPanel != null) settingPanel.SetActive(false);
    }
    private void Update() => RefreshInput();
    private void RefreshInput()
    {
        var next = GameInputManager.instance;
        if (next == input) return;
        UnbindInput(); input = next;
        if (input == null) return;
        input.OnSettingPressed += ToggleSetting;
    }
    public void OpenSetting(bool isTimeStop)
    {
        if (settingPanel == null || IsOpen) return;
        RefreshInput();
        GamePause.Acquire(this, isTimeStop);
        settingPanel.SetActive(true);
    }
    public void CloseSetting()
    {
        if (settingPanel != null) settingPanel.SetActive(false);
        GamePause.Release(this);
    }
    public void ToggleSetting() { if (IsOpen) CloseSetting(); else OpenSetting(pauseWhenToggled); }
    private void UnbindInput()
    {
        if (input == null) return;
        input.OnSettingPressed -= ToggleSetting;
        input = null;
    }
    private void OnDisable() { CloseSetting(); UnbindInput(); }
    private void OnDestroy() { GamePause.Release(this); if (instance == this) instance = null; }
}
