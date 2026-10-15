using UnityEngine;

public class SettingWindowController : MonoBehaviour
{
    public static SettingWindowController instance;
    public GameObject settingPanel;
    public bool pauseWhenToggled = true;
    public bool IsOpen => settingPanel != null && settingPanel.activeSelf;
    private bool paused, blockedBeforeOpen;
    private float previousTimeScale = 1f;
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
        if (IsOpen) BlockGameplay();
    }
    public void OpenSetting(bool isTimeStop)
    {
        if (settingPanel == null || IsOpen) return;
        RefreshInput();
        previousTimeScale = Time.timeScale; paused = isTimeStop;
        if (paused) Time.timeScale = 0f;
        BlockGameplay(); settingPanel.SetActive(true);
    }
    public void CloseSetting()
    {
        if (!IsOpen) return;
        settingPanel.SetActive(false);
        if (paused) Time.timeScale = previousTimeScale;
        paused = false;
        if (input != null) input.BlockGameplayInput(blockedBeforeOpen);
    }
    public void ToggleSetting() { if (IsOpen) CloseSetting(); else OpenSetting(pauseWhenToggled); }
    private void BlockGameplay()
    {
        if (input == null) return;
        blockedBeforeOpen = input.IsBlocked(GameInputBlockType.Gameplay); input.BlockGameplayInput(true);
    }
    private void UnbindInput()
    {
        if (input == null) return;
        input.OnSettingPressed -= ToggleSetting;
        if (IsOpen) input.BlockGameplayInput(blockedBeforeOpen);
        input = null;
    }
    private void OnDisable() { CloseSetting(); UnbindInput(); }
    private void OnDestroy() { if (instance == this) instance = null; }
}
