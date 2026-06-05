using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioSettingUI : MonoBehaviour
{
    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Audio Text")]
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI bgmVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    [Header("Mute Buttons")]
    public Button masterMuteButton;
    public Button bgmMuteButton;
    public Button sfxMuteButton;

    [Header("Mute Button Text")]
    public TextMeshProUGUI masterMuteText;
    public TextMeshProUGUI bgmMuteText;
    public TextMeshProUGUI sfxMuteText;

    private bool isInitialized = false;

    private float lastMasterVolume = 1f;
    private float lastBgmVolume = 1f;
    private float lastSfxVolume = 1f;

    private const float MUTE_THRESHOLD = 0.001f;
    private const float DEFAULT_RESTORE_VOLUME = 1f;

    private void OnEnable()
    {
        BindEvents();

        if (SettingManager.instance != null)
            SettingManager.instance.RegisterAudioSettingUI(this);
    }

    private void Start()
    {
        Init();
    }

    private void OnDisable()
    {
        if (SettingManager.instance != null)
            SettingManager.instance.UnregisterAudioSettingUI(this);
    }

    private void OnDestroy()
    {
        if (SettingManager.instance != null)
            SettingManager.instance.UnregisterAudioSettingUI(this);
    }

    public void Init()
    {
        if (SettingManager.instance == null)
        {
            Debug.LogWarning("SettingManager가 없습니다.");
            return;
        }

        SetupSliderRanges();
        BindEvents();
        RefreshFromSetting(SettingManager.instance.GetSetting());

        isInitialized = true;
    }

    private void SetupSliderRanges()
    {
        SetupVolumeSlider(masterVolumeSlider);
        SetupVolumeSlider(bgmVolumeSlider);
        SetupVolumeSlider(sfxVolumeSlider);
    }

    private void SetupVolumeSlider(Slider slider)
    {
        if (slider == null)
            return;

        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
    }

    public void RefreshFromSetting(GameSettingData setting)
    {
        if (setting == null)
            return;

        bool previousInitialized = isInitialized;
        isInitialized = false;

        if (setting.masterVolume > MUTE_THRESHOLD)
            lastMasterVolume = setting.masterVolume;

        if (setting.bgmVolume > MUTE_THRESHOLD)
            lastBgmVolume = setting.bgmVolume;

        if (setting.sfxVolume > MUTE_THRESHOLD)
            lastSfxVolume = setting.sfxVolume;

        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(setting.masterVolume);

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(setting.bgmVolume);

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(setting.sfxVolume);

        UpdateMasterVolumeText(setting.masterVolume);
        UpdateBgmVolumeText(setting.bgmVolume);
        UpdateSfxVolumeText(setting.sfxVolume);

        UpdateMuteTexts(setting);

        isInitialized = previousInitialized;
    }

    private void BindEvents()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (bgmVolumeSlider != null)
        {
            bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);
            sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);
        }

        if (masterMuteButton != null)
        {
            masterMuteButton.onClick.RemoveListener(ToggleMasterMute);
            masterMuteButton.onClick.AddListener(ToggleMasterMute);
        }

        if (bgmMuteButton != null)
        {
            bgmMuteButton.onClick.RemoveListener(ToggleBgmMute);
            bgmMuteButton.onClick.AddListener(ToggleBgmMute);
        }

        if (sfxMuteButton != null)
        {
            sfxMuteButton.onClick.RemoveListener(ToggleSfxMute);
            sfxMuteButton.onClick.AddListener(ToggleSfxMute);
        }
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (!isInitialized)
            return;

        if (value > MUTE_THRESHOLD)
            lastMasterVolume = value;

        if (SettingManager.instance != null)
            SettingManager.instance.SetMasterVolume(value);

        UpdateMasterVolumeText(value);
        UpdateMasterMuteText(value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (!isInitialized)
            return;

        if (value > MUTE_THRESHOLD)
            lastBgmVolume = value;

        if (SettingManager.instance != null)
            SettingManager.instance.SetBgmVolume(value);

        UpdateBgmVolumeText(value);
        UpdateBgmMuteText(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (!isInitialized)
            return;

        if (value > MUTE_THRESHOLD)
            lastSfxVolume = value;

        if (SettingManager.instance != null)
            SettingManager.instance.SetSfxVolume(value);

        UpdateSfxVolumeText(value);
        UpdateSfxMuteText(value);
    }

    private void ToggleMasterMute()
    {
        if (SettingManager.instance == null)
            return;

        float currentValue = SettingManager.instance.GetSetting().masterVolume;

        if (currentValue > MUTE_THRESHOLD)
        {
            lastMasterVolume = currentValue;
            SetMasterVolumeByUI(0f);
        }
        else
        {
            SetMasterVolumeByUI(GetRestoreVolume(lastMasterVolume));
        }
    }

    private void ToggleBgmMute()
    {
        if (SettingManager.instance == null)
            return;

        float currentValue = SettingManager.instance.GetSetting().bgmVolume;

        if (currentValue > MUTE_THRESHOLD)
        {
            lastBgmVolume = currentValue;
            SetBgmVolumeByUI(0f);
        }
        else
        {
            SetBgmVolumeByUI(GetRestoreVolume(lastBgmVolume));
        }
    }

    private void ToggleSfxMute()
    {
        if (SettingManager.instance == null)
            return;

        float currentValue = SettingManager.instance.GetSetting().sfxVolume;

        if (currentValue > MUTE_THRESHOLD)
        {
            lastSfxVolume = currentValue;
            SetSfxVolumeByUI(0f);
        }
        else
        {
            SetSfxVolumeByUI(GetRestoreVolume(lastSfxVolume));
        }
    }

    private void SetMasterVolumeByUI(float value)
    {
        if (masterVolumeSlider != null)
            masterVolumeSlider.SetValueWithoutNotify(value);

        SettingManager.instance.SetMasterVolume(value);

        UpdateMasterVolumeText(value);
        UpdateMasterMuteText(value);
    }

    private void SetBgmVolumeByUI(float value)
    {
        if (bgmVolumeSlider != null)
            bgmVolumeSlider.SetValueWithoutNotify(value);

        SettingManager.instance.SetBgmVolume(value);

        UpdateBgmVolumeText(value);
        UpdateBgmMuteText(value);
    }

    private void SetSfxVolumeByUI(float value)
    {
        if (sfxVolumeSlider != null)
            sfxVolumeSlider.SetValueWithoutNotify(value);

        SettingManager.instance.SetSfxVolume(value);

        UpdateSfxVolumeText(value);
        UpdateSfxMuteText(value);
    }

    private float GetRestoreVolume(float lastVolume)
    {
        if (lastVolume <= MUTE_THRESHOLD)
            return DEFAULT_RESTORE_VOLUME;

        return Mathf.Clamp01(lastVolume);
    }

    private void UpdateMasterVolumeText(float value)
    {
        if (masterVolumeText == null)
            return;

        masterVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateBgmVolumeText(float value)
    {
        if (bgmVolumeText == null)
            return;

        bgmVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateSfxVolumeText(float value)
    {
        if (sfxVolumeText == null)
            return;

        sfxVolumeText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateMuteTexts(GameSettingData setting)
    {
        UpdateMasterMuteText(setting.masterVolume);
        UpdateBgmMuteText(setting.bgmVolume);
        UpdateSfxMuteText(setting.sfxVolume);
    }

    private void UpdateMasterMuteText(float value)
    {
        if (masterMuteText == null)
            return;

        masterMuteText.text = value <= MUTE_THRESHOLD ? "ON" : "OFF";
    }

    private void UpdateBgmMuteText(float value)
    {
        if (bgmMuteText == null)
            return;

        bgmMuteText.text = value <= MUTE_THRESHOLD ? "ON" : "OFF";
    }

    private void UpdateSfxMuteText(float value)
    {
        if (sfxMuteText == null)
            return;

        sfxMuteText.text = value <= MUTE_THRESHOLD ? "ON" : "OFF";
    }
}