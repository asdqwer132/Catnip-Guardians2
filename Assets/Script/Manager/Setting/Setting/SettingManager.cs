using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;

    [Header("Current Setting")]
    public GameSettingData setting = new GameSettingData();

    [Header("Managers")]
    public AudioManager audioManager;
    public CursorChanger cursorChanger;

    private SettingUI currentSettingUI;
    private AudioSettingUI currentAudioSettingUI;

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
    }

    private void Start()
    {
        ApplyAll();
    }

    public void RegisterAudioManager(AudioManager manager)
    {
        audioManager = manager;
        ApplyAudio();
    }

    public void RegisterCursorChanger(CursorChanger changer)
    {
        cursorChanger = changer;
        ApplyCursor();
    }

    public void RegisterSettingUI(SettingUI settingUI)
    {
        currentSettingUI = settingUI;

        if (currentSettingUI != null)
            currentSettingUI.RefreshFromSetting(setting);
    }

    public void UnregisterSettingUI(SettingUI settingUI)
    {
        if (currentSettingUI == settingUI)
            currentSettingUI = null;
    }

    public void RegisterAudioSettingUI(AudioSettingUI audioSettingUI)
    {
        currentAudioSettingUI = audioSettingUI;

        if (currentAudioSettingUI != null)
            currentAudioSettingUI.RefreshFromSetting(setting);
    }

    public void UnregisterAudioSettingUI(AudioSettingUI audioSettingUI)
    {
        if (currentAudioSettingUI == audioSettingUI)
            currentAudioSettingUI = null;
    }

    public void OpenSetting()
    {
        if (currentSettingUI == null)
        {
            Debug.LogWarning("현재 씬에 SettingUI가 없습니다.");
            return;
        }

        currentSettingUI.OpenSetting();
    }

    public void CloseSetting()
    {
        if (currentSettingUI == null)
            return;

        currentSettingUI.CloseSetting();
    }

    public void ToggleSetting()
    {
        if (currentSettingUI == null)
        {
            Debug.LogWarning("현재 씬에 SettingUI가 없습니다.");
            return;
        }

        currentSettingUI.ToggleSetting();
    }

    public GameSettingData GetSetting()
    {
        return setting;
    }

    public void Load()
    {
        setting.masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME, 1f);
        setting.bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME, 1f);
        setting.sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME, 1f);

        setting.cursorScale = PlayerPrefs.GetFloat(CURSOR_SCALE, 1f);
        setting.cursorScale = Mathf.Clamp(setting.cursorScale, 0.5f, 3f);

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
        RefreshAllSettingUI();
    }

    private void RefreshAllSettingUI()
    {
        if (currentSettingUI != null)
            currentSettingUI.RefreshFromSetting(setting);

        if (currentAudioSettingUI != null)
            currentAudioSettingUI.RefreshFromSetting(setting);
    }

    public void SetMasterVolume(float value)
    {
        setting.masterVolume = Mathf.Clamp01(value);
        ApplyAudio();
        Save();

        if (currentAudioSettingUI != null)
            currentAudioSettingUI.RefreshFromSetting(setting);
    }

    public void SetBgmVolume(float value)
    {
        setting.bgmVolume = Mathf.Clamp01(value);
        ApplyAudio();
        Save();

        if (currentAudioSettingUI != null)
            currentAudioSettingUI.RefreshFromSetting(setting);
    }

    public void SetSfxVolume(float value)
    {
        setting.sfxVolume = Mathf.Clamp01(value);
        ApplyAudio();
        Save();

        if (currentAudioSettingUI != null)
            currentAudioSettingUI.RefreshFromSetting(setting);
    }

    public void SetCursorScale(float value)
    {
        setting.cursorScale = Mathf.Clamp(value, 0.5f, 3f);
        ApplyCursor();
        Save();

        if (currentSettingUI != null)
            currentSettingUI.RefreshFromSetting(setting);
    }

    public void SetIndicatorSpriteSize(float value)
    {
        int index = Mathf.RoundToInt(value);
        index = Mathf.Clamp(index, 0, 2);

        setting.indicatorSpriteSize = index;
        Save();

        if (currentSettingUI != null)
            currentSettingUI.RefreshFromSetting(setting);
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

        if (currentSettingUI != null)
            currentSettingUI.RefreshFromSetting(setting);
    }

    public void SetShowHealthBar(bool value)
    {
        setting.showHealthBar = value;
        Save();

        if (currentSettingUI != null)
            currentSettingUI.RefreshFromSetting(setting);
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