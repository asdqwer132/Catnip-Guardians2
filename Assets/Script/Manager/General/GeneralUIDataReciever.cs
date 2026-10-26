using UnityEngine;

public class GeneralUIDataReciever : MonoBehaviour, ISettingChangeListener
{
    [SerializeField] private ObjectToggleButton toggleButton;
    private GeneralUIDataSender boundSender;
    private bool? pendingActive;
    private SettingManager boundSettings;

    private void OnEnable()
    {
        GeneralUIDataSender.InstanceChanged += BindCurrentSender;
        SettingManager.InstanceChanged += BindSettings;
        BindSettings();
        BindCurrentSender();
    }

    private void OnDisable()
    {
        GeneralUIDataSender.InstanceChanged -= BindCurrentSender;
        SettingManager.InstanceChanged -= BindSettings;
        if (boundSettings != null) boundSettings.RemoveListener(this);
        boundSettings = null;
        boundSender = null;
        toggleButton = null;
    }

    private void BindSettings()
    {
        if (boundSettings != null) boundSettings.RemoveListener(this);
        boundSettings = SettingManager.instance;
        if (boundSettings != null) boundSettings.AddListener(this);
    }

    public void OnSettingChanged(GameSettingData setting, SettingChangeType changeType)
    {
        if (setting == null || (changeType != SettingChangeType.All && changeType != SettingChangeType.ShowBuff)) return;
        pendingActive = setting.showBuff;
        BindCurrentSender();
    }

    private void BindCurrentSender()
    {
        if (boundSettings != null) pendingActive = boundSettings.setting.showBuff;
        Initialize(GeneralUIDataSender.instance);
    }

    public void Initialize(GeneralUIDataSender sender)
    {
        boundSender = sender;
        toggleButton = sender != null ? sender.GetToggleBtn() : null;
        if (!CanApply || !pendingActive.HasValue) return;
        bool active = pendingActive.Value;
        pendingActive = null;
        toggleButton.SetObjectActive(active);
    }

    private bool CanApply => boundSender != null && boundSender.isActiveAndEnabled &&
        boundSender.IsReady && toggleButton != null;

    public void TryUIDataToggleButton(bool isActive)
    {
        // 송신 씬이 아직 없으면 마지막 요청을 보관합니다.
        if (boundSettings != null) boundSettings.SetShowBuff(isActive);
        pendingActive = isActive;
        BindCurrentSender();
    }
}
