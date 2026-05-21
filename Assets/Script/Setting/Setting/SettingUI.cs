using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingPanel;

    [Header("Pause")]
    public bool pauseWhenOpen = true;
    private float previousTimeScale = 1f;
    private bool isOpen = false;

    [Header("Audio Sliders")]
    public Slider masterVolumeSlider;
    public Slider bgmVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Cursor")]
    public Slider cursorScaleSlider;
    public TextMeshProUGUI cursorScaleText;

    [Header("Indicator")]
    public Slider indicatorSpriteSizeSlider;
    public TextMeshProUGUI indicatorSpriteSizeText;

    [Header("Gameplay")]
    public Toggle damagePopupToggle;
    public Toggle healthBarToggle;

    private bool isInitialized = false;

    private void Start()
    {
        Init();

        if (settingPanel != null)
            settingPanel.SetActive(false);

        isOpen = false;
    }

    public void Init()
    {
        if (SettingManager.instance == null)
        {
            Debug.LogWarning("SettingManager가 없습니다.");
            return;
        }

        GameSettingData setting = SettingManager.instance.GetSetting();

        isInitialized = false;

        if (masterVolumeSlider != null)
            masterVolumeSlider.value = setting.masterVolume;

        if (bgmVolumeSlider != null)
            bgmVolumeSlider.value = setting.bgmVolume;

        if (sfxVolumeSlider != null)
            sfxVolumeSlider.value = setting.sfxVolume;

        if (cursorScaleSlider != null)
        {
            cursorScaleSlider.minValue = 0.5f;
            cursorScaleSlider.maxValue = 3f;
            cursorScaleSlider.wholeNumbers = false;
            cursorScaleSlider.value = setting.cursorScale;
        }

        if (indicatorSpriteSizeSlider != null)
        {
            indicatorSpriteSizeSlider.minValue = 0;
            indicatorSpriteSizeSlider.maxValue = 2;
            indicatorSpriteSizeSlider.wholeNumbers = true;
            indicatorSpriteSizeSlider.value = setting.indicatorSpriteSize;
        }

        if (damagePopupToggle != null)
            damagePopupToggle.isOn = setting.showDamagePopup;

        if (healthBarToggle != null)
            healthBarToggle.isOn = setting.showHealthBar;

        UpdateCursorScaleText(setting.cursorScale);
        UpdateIndicatorSpriteSizeText(setting.indicatorSpriteSize);

        BindEvents();

        isInitialized = true;
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

        if (cursorScaleSlider != null)
        {
            cursorScaleSlider.onValueChanged.RemoveListener(OnCursorScaleChanged);
            cursorScaleSlider.onValueChanged.AddListener(OnCursorScaleChanged);
        }

        if (indicatorSpriteSizeSlider != null)
        {
            indicatorSpriteSizeSlider.onValueChanged.RemoveListener(OnIndicatorSpriteSizeChanged);
            indicatorSpriteSizeSlider.onValueChanged.AddListener(OnIndicatorSpriteSizeChanged);
        }

        if (damagePopupToggle != null)
        {
            damagePopupToggle.onValueChanged.RemoveListener(OnDamagePopupChanged);
            damagePopupToggle.onValueChanged.AddListener(OnDamagePopupChanged);
        }

        if (healthBarToggle != null)
        {
            healthBarToggle.onValueChanged.RemoveListener(OnHealthBarChanged);
            healthBarToggle.onValueChanged.AddListener(OnHealthBarChanged);
        }
    }

    public void OpenSetting()
    {
        if (settingPanel != null)
            settingPanel.SetActive(true);

        isOpen = true;

        if (pauseWhenOpen)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }
    }

    public void CloseSetting()
    {
        if (settingPanel != null)
            settingPanel.SetActive(false);

        isOpen = false;

        if (pauseWhenOpen)
        {
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
        }
    }

    public void ToggleSetting()
    {
        if (isOpen)
            CloseSetting();
        else
            OpenSetting();
    }

    private void OnMasterVolumeChanged(float value)
    {
        if (!isInitialized)
            return;

        if (SettingManager.instance != null)
            SettingManager.instance.SetMasterVolume(value);
    }

    private void OnBgmVolumeChanged(float value)
    {
        if (!isInitialized)
            return;

        if (SettingManager.instance != null)
            SettingManager.instance.SetBgmVolume(value);
    }

    private void OnSfxVolumeChanged(float value)
    {
        if (!isInitialized)
            return;

        if (SettingManager.instance != null)
            SettingManager.instance.SetSfxVolume(value);
    }

    private void OnCursorScaleChanged(float value)
    {
        if (!isInitialized)
            return;

        if (SettingManager.instance != null)
            SettingManager.instance.SetCursorScale(value);

        UpdateCursorScaleText(value);
    }

    private void OnIndicatorSpriteSizeChanged(float value)
    {
        if (!isInitialized)
            return;

        int index = Mathf.RoundToInt(value);

        if (SettingManager.instance != null)
            SettingManager.instance.SetIndicatorSpriteSize(index);

        UpdateIndicatorSpriteSizeText(index);
    }

    private void OnDamagePopupChanged(bool value)
    {
        if (!isInitialized)
            return;

        if (SettingManager.instance != null)
            SettingManager.instance.SetShowDamagePopup(value);
    }

    private void OnHealthBarChanged(bool value)
    {
        if (!isInitialized)
            return;

        if (SettingManager.instance != null)
            SettingManager.instance.SetShowHealthBar(value);
    }

    private void UpdateCursorScaleText(float value)
    {
        if (cursorScaleText == null)
            return;

        cursorScaleText.text = Mathf.RoundToInt(value * 100f) + "%";
    }

    private void UpdateIndicatorSpriteSizeText(int index)
    {
        if (indicatorSpriteSizeText == null)
            return;

        switch (index)
        {
            case 0:
                indicatorSpriteSizeText.text = "S";
                break;

            case 1:
                indicatorSpriteSizeText.text = "M";
                break;

            case 2:
                indicatorSpriteSizeText.text = "B";
                break;

            default:
                indicatorSpriteSizeText.text = "M";
                break;
        }
    }
}