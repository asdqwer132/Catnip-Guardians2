using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingUI : MonoBehaviour, ISettingChangeListener
{
    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Audio Text")]
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI bgmVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    private bool isInitialized;

    private void Awake()
    {
        BindEvents();
    }

    private void Start()
    {
        Init();
    }

    private void BindEvents()
    {
        if (isInitialized)
            return;

        if (masterVolumeSlider != null)
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

        isInitialized = true;
    }

    public void Init()
    {
        if (SettingManager.instance == null)
            return;

        RefreshFromSetting(SettingManager.instance.GetSetting());
    }

    public void OnSettingChanged(GameSettingData setting, SettingChangeType changeType)
    {
        switch (changeType)
        {
            case SettingChangeType.All:
            case SettingChangeType.MasterVolume:
            case SettingChangeType.BgmVolume:
            case SettingChangeType.SfxVolume:
                RefreshFromSetting(setting);
                break;
        }
    }

    public void RefreshFromSetting(GameSettingData setting)
    {
        if (setting == null)
            return;

        SetSliderWithoutNotify(masterVolumeSlider, setting.masterVolume);
        SetSliderWithoutNotify(bgmVolumeSlider, setting.bgmVolume);
        SetSliderWithoutNotify(sfxVolumeSlider, setting.sfxVolume);

        if (masterVolumeText != null)
            masterVolumeText.text = ToPercentText(setting.masterVolume);

        if (bgmVolumeText != null)
            bgmVolumeText.text = ToPercentText(setting.bgmVolume);

        if (sfxVolumeText != null)
            sfxVolumeText.text = ToPercentText(setting.sfxVolume);
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetMasterVolume(value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetSfxVolume(value);
    }

    private void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.SetValueWithoutNotify(value);
    }

    private string ToPercentText(float value)
    {
        int percent = Mathf.RoundToInt(value * 100f);
        return $"{percent}%";
    }
}