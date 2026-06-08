using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour, ISettingChangeListener
{
    [Header("Panel")]
    public GameObject settingPanel;

    [Header("Cursor")]
    public Slider cursorScaleSlider;
    public TextMeshProUGUI cursorScaleText;

    [Header("Indicator")]
    public Slider indicatorSpriteSizeSlider;
    public TextMeshProUGUI indicatorSpriteSizeText;

    [Header("Display")]
    public Toggle showDamagePopupToggle;
    public Toggle showHealthBarToggle;

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

        if (cursorScaleSlider != null)
            cursorScaleSlider.onValueChanged.AddListener(OnCursorScaleChanged);

        if (indicatorSpriteSizeSlider != null)
            indicatorSpriteSizeSlider.onValueChanged.AddListener(OnIndicatorSpriteSizeChanged);

        if (showDamagePopupToggle != null)
            showDamagePopupToggle.onValueChanged.AddListener(OnShowDamagePopupChanged);

        if (showHealthBarToggle != null)
            showHealthBarToggle.onValueChanged.AddListener(OnShowHealthBarChanged);

        isInitialized = true;
    }

    public void Init()
    {
        if (SettingManager.instance == null)
            return;

        RefreshFromSetting(SettingManager.instance.GetSetting());
    }

    public void ToggleSetting()
    {
        if (settingPanel == null)
            return;

        bool nextState = !settingPanel.activeSelf;
        settingPanel.SetActive(nextState);

        if (nextState && SettingManager.instance != null)
            RefreshFromSetting(SettingManager.instance.GetSetting());
    }

    public void OnSettingChanged(GameSettingData setting, SettingChangeType changeType)
    {
        switch (changeType)
        {
            case SettingChangeType.All:
            case SettingChangeType.CursorScale:
            case SettingChangeType.IndicatorSpriteSize:
            case SettingChangeType.ShowDamagePopup:
            case SettingChangeType.ShowHealthBar:
                RefreshFromSetting(setting);
                break;
        }
    }

    public void RefreshFromSetting(GameSettingData setting)
    {
        if (setting == null)
            return;

        SetSliderWithoutNotify(cursorScaleSlider, setting.cursorScale);
        SetSliderWithoutNotify(indicatorSpriteSizeSlider, setting.indicatorSpriteSize);

        SetToggleWithoutNotify(showDamagePopupToggle, setting.showDamagePopup);
        SetToggleWithoutNotify(showHealthBarToggle, setting.showHealthBar);

        if (cursorScaleText != null)
            cursorScaleText.text = $"{setting.cursorScale:0.0}";

        if (indicatorSpriteSizeText != null)
            indicatorSpriteSizeText.text = GetIndicatorSizeText(setting.indicatorSpriteSize);
    }

    private void OnCursorScaleChanged(float value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetCursorScale(value);
    }

    private void OnIndicatorSpriteSizeChanged(float value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetIndicatorSpriteSize(value);
    }

    private void OnShowDamagePopupChanged(bool value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetShowDamagePopup(value);
    }

    private void OnShowHealthBarChanged(bool value)
    {
        if (SettingManager.instance == null)
            return;

        SettingManager.instance.SetShowHealthBar(value);
    }

    private void SetSliderWithoutNotify(Slider slider, float value)
    {
        if (slider == null)
            return;

        slider.SetValueWithoutNotify(value);
    }

    private void SetToggleWithoutNotify(Toggle toggle, bool value)
    {
        if (toggle == null)
            return;

        toggle.SetIsOnWithoutNotify(value);
    }

    private string GetIndicatorSizeText(int index)
    {
        switch (index)
        {
            case 0:
                return "S";
            case 1:
                return "M";
            case 2:
                return "L";
            default:
                return "M";
        }
    }
}