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
    public Toggle showBuffToggle;

    [Header("Language")]
    [Tooltip("직접 만든 English(0), Korean(1) 드롭다운을 연결합니다. 항목은 코드로 생성하지 않습니다.")]
    public TMP_Dropdown languageDropdown;
    private LanguageManager languageManager;
    private bool isInitialized;

    private void Awake()
    {
        BindEvents();
    }

    private void OnEnable()
    {
        LanguageManager.InstanceChanged += BindLanguageManager;
        BindLanguageManager();
    }
    private void OnDisable()
    {
        LanguageManager.InstanceChanged -= BindLanguageManager;
        if (languageManager != null) languageManager.LanguageChanged -= RefreshLanguage;
        languageManager = null;
    }
    private void BindLanguageManager()
    {
        if (languageManager != null) languageManager.LanguageChanged -= RefreshLanguage;
        languageManager = LanguageManager.instance;
        if (languageManager != null) languageManager.LanguageChanged += RefreshLanguage;
        RefreshLanguage(LanguageManager.CurrentLanguage);
    }
    private void RefreshLanguage(language selected)
    {
        if (languageDropdown != null) languageDropdown.SetValueWithoutNotify((int)selected);
    }
    public void OnLanguageChanged(int value)
    {
        if (LanguageManager.instance != null) LanguageManager.instance.SetLanguage(value);
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

        if (showBuffToggle != null)
            showBuffToggle.onValueChanged.AddListener(OnShowBuffChanged);

        if (languageDropdown != null) languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
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
            case SettingChangeType.ShowBuff:
                RefreshFromSetting(setting);
                break;
        }
    }

    public void RefreshFromSetting(GameSettingData setting)
    {
        if (setting == null)
            return;

        RefreshLanguage(LanguageManager.CurrentLanguage);
        SetSliderWithoutNotify(cursorScaleSlider, setting.cursorScale);
        SetSliderWithoutNotify(indicatorSpriteSizeSlider, setting.indicatorSpriteSize);

        SetToggleWithoutNotify(showDamagePopupToggle, setting.showDamagePopup);
        SetToggleWithoutNotify(showHealthBarToggle, setting.showHealthBar);
        SetToggleWithoutNotify(showBuffToggle, setting.showBuff);

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

    private void OnShowBuffChanged(bool value)
    {
        if (SettingManager.instance != null) SettingManager.instance.SetShowBuff(value);
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