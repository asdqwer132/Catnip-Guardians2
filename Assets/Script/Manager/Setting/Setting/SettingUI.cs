using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingUI : MonoBehaviour
{
    [Header("Panel")]
    public GameObject settingPanel;

    [Header("Pause")]
    public bool pauseWhenOpen = true;

    [Header("Cursor")]
    public Slider cursorScaleSlider;
    public TextMeshProUGUI cursorScaleText;

    [Header("Indicator")]
    public Slider indicatorSpriteSizeSlider;
    public TextMeshProUGUI indicatorSpriteSizeText;

    [Header("Gameplay")]
    public Toggle damagePopupToggle;
    public Toggle healthBarToggle;

    private float previousTimeScale = 1f;
    private bool isOpen = false;
    private bool isInitialized = false;

    private void OnEnable()
    {
        BindEvents();

        if (SettingManager.instance != null)
            SettingManager.instance.RegisterSettingUI(this);
    }

    private void Start()
    {
        Init();

        if (settingPanel != null)
            settingPanel.SetActive(false);

        isOpen = false;
    }

    private void OnDisable()
    {
        if (SettingManager.instance != null)
            SettingManager.instance.UnregisterSettingUI(this);
    }

    private void OnDestroy()
    {
        if (pauseWhenOpen && isOpen)
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;

        if (SettingManager.instance != null)
            SettingManager.instance.UnregisterSettingUI(this);
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
        if (cursorScaleSlider != null)
        {
            cursorScaleSlider.minValue = 0.5f;
            cursorScaleSlider.maxValue = 3f;
            cursorScaleSlider.wholeNumbers = false;
        }

        if (indicatorSpriteSizeSlider != null)
        {
            indicatorSpriteSizeSlider.minValue = 0;
            indicatorSpriteSizeSlider.maxValue = 2;
            indicatorSpriteSizeSlider.wholeNumbers = true;
        }
    }

    public void RefreshFromSetting(GameSettingData setting)
    {
        if (setting == null)
            return;

        bool previousInitialized = isInitialized;
        isInitialized = false;

        if (cursorScaleSlider != null)
            cursorScaleSlider.SetValueWithoutNotify(setting.cursorScale);

        if (indicatorSpriteSizeSlider != null)
            indicatorSpriteSizeSlider.SetValueWithoutNotify(setting.indicatorSpriteSize);

        if (damagePopupToggle != null)
            damagePopupToggle.SetIsOnWithoutNotify(setting.showDamagePopup);

        if (healthBarToggle != null)
            healthBarToggle.SetIsOnWithoutNotify(setting.showHealthBar);

        UpdateCursorScaleText(setting.cursorScale);
        UpdateIndicatorSpriteSizeText(setting.indicatorSpriteSize);

        isInitialized = previousInitialized;
    }

    private void BindEvents()
    {
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
        if (isOpen)
            return;

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
        if (!isOpen)
            return;

        if (settingPanel != null)
            settingPanel.SetActive(false);

        isOpen = false;

        if (pauseWhenOpen)
            Time.timeScale = previousTimeScale <= 0f ? 1f : previousTimeScale;
    }

    public void ToggleSetting()
    {
        if (isOpen)
            CloseSetting();
        else
            OpenSetting();
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