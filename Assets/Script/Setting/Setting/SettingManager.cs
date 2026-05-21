using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;

    [Header("Current Setting")]
    public GameSettingData setting = new GameSettingData();

    [Header("Managers")]
    public AudioManager audioManager;
    public CursorChanger cursorChanger;

    private const string MASTER_VOLUME = "Setting_MasterVolume";
    private const string BGM_VOLUME = "Setting_BgmVolume";
    private const string SFX_VOLUME = "Setting_SfxVolume";

    private const string CURSOR_SCALE = "Setting_CursorScale";

    private const string INDICATOR_SPRITE_SIZE = "Setting_IndicatorSpriteSize";

    private const string SHOW_DAMAGE_POPUP = "Setting_ShowDamagePopup";
    private const string SHOW_HEALTH_BAR = "Setting_ShowHealthBar";

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
        ApplyAll();
    }

    public void Load()
    {
        setting.masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME, 1f);
        setting.bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME, 1f);
        setting.sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME, 1f);

        setting.cursorScale = PlayerPrefs.GetFloat(CURSOR_SCALE, 1f);

        setting.indicatorSpriteSize = PlayerPrefs.GetInt(INDICATOR_SPRITE_SIZE, 1);
        setting.indicatorSpriteSize = Mathf.Clamp(setting.indicatorSpriteSize, 0, 2);

        setting.showDamagePopup = PlayerPrefs.GetInt(SHOW_DAMAGE_POPUP, 1) == 1;
        setting.showHealthBar = PlayerPrefs.GetInt(SHOW_HEALTH_BAR, 1) == 1;
    }

    public void Save()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME, setting.masterVolume);
        PlayerPrefs.SetFloat(BGM_VOLUME, setting.bgmVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME, setting.sfxVolume);

        PlayerPrefs.SetFloat(CURSOR_SCALE, setting.cursorScale);

        PlayerPrefs.SetInt(INDICATOR_SPRITE_SIZE, setting.indicatorSpriteSize);

        PlayerPrefs.SetInt(SHOW_DAMAGE_POPUP, setting.showDamagePopup ? 1 : 0);
        PlayerPrefs.SetInt(SHOW_HEALTH_BAR, setting.showHealthBar ? 1 : 0);

        PlayerPrefs.Save();
    }

    public void ApplyAll()
    {
        ApplyAudio();
        ApplyCursor();

        // 중요:
        // 여기서 TargetRangeIndicator를 찾지 않는다.
        // 인디케이터는 프리팹 생성 시 자기 자신이 설정을 읽는다.
    }

    public GameSettingData GetSetting()
    {
        return setting;
    }

    public void SetMasterVolume(float value)
    {
        setting.masterVolume = Mathf.Clamp01(value);
        ApplyAudio();
        Save();
    }

    public void SetBgmVolume(float value)
    {
        setting.bgmVolume = Mathf.Clamp01(value);
        ApplyAudio();
        Save();
    }

    public void SetSfxVolume(float value)
    {
        setting.sfxVolume = Mathf.Clamp01(value);
        ApplyAudio();
        Save();
    }

    public void SetCursorScale(float value)
    {
        setting.cursorScale = Mathf.Clamp(value, 0.5f, 3f);
        ApplyCursor();
        Save();
    }

    public void SetIndicatorSpriteSize(float value)
    {
        int index = Mathf.RoundToInt(value);
        index = Mathf.Clamp(index, 0, 2);

        setting.indicatorSpriteSize = index;

        Save();
    }

    public IndicatorSpriteSize GetIndicatorSpriteSize()
    {
        int index = Mathf.Clamp(setting.indicatorSpriteSize, 0, 2);
        return (IndicatorSpriteSize)index;
    }

    public void SetShowDamagePopup(bool value)
    {
        setting.showDamagePopup = value;
        Save();
    }

    public void SetShowHealthBar(bool value)
    {
        setting.showHealthBar = value;
        Save();
    }

    private void ApplyAudio()
    {
        if (audioManager == null && AudioManager.instance != null)
            audioManager = AudioManager.instance;

        if (audioManager == null)
            return;

        audioManager.SetMasterVolume(setting.masterVolume);
        audioManager.SetBgmVolume(setting.bgmVolume);
        audioManager.SetSfxVolume(setting.sfxVolume);
    }

    private void ApplyCursor()
    {
        if (cursorChanger == null && CursorChanger.instance != null)
            cursorChanger = CursorChanger.instance;

        if (cursorChanger == null)
            return;

        cursorChanger.SetCursorScale(setting.cursorScale);
    }
}