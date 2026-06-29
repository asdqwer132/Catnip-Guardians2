using System.Collections.Generic;
using UnityEngine;

public class SettingManager : MonoBehaviour
{
    public static SettingManager instance;

    [Header("Current Setting")]
    public GameSettingData setting = new GameSettingData();

    [Header("Managers")]
    public AudioManager audioManager;
    public CursorChanger cursorChanger;

    [Header("Setting Change Listeners")]
    [SerializeField] private List<MonoBehaviour> settingListeners = new List<MonoBehaviour>();

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
        BroadcastSettingChanged(SettingChangeType.All);
        SettingWindowController.instance.CloseSetting(false);
    }

    public GameSettingData GetSetting()
    {
        return setting;
    }

    public void AddListener(MonoBehaviour listener)
    {
        if (listener == null)
            return;

        if (listener is not ISettingChangeListener settingListener)
        {
            Debug.LogWarning($"{listener.name}은 ISettingChangeListener를 구현하지 않았습니다.");
            return;
        }

        if (!settingListeners.Contains(listener))
            settingListeners.Add(listener);

        settingListener.OnSettingChanged(setting, SettingChangeType.All);
    }

    public void RemoveListener(MonoBehaviour listener)
    {
        if (listener == null)
            return;

        settingListeners.Remove(listener);
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
    }

    public void SetMasterVolume(float value)
    {
        float newValue = Mathf.Clamp01(value);

        if (Mathf.Approximately(setting.masterVolume, newValue))
            return;

        setting.masterVolume = newValue;
        ApplyAudio();
        Save();
        BroadcastSettingChanged(SettingChangeType.MasterVolume);
    }

    public void SetBgmVolume(float value)
    {
        float newValue = Mathf.Clamp01(value);

        if (Mathf.Approximately(setting.bgmVolume, newValue))
            return;

        setting.bgmVolume = newValue;
        ApplyAudio();
        Save();
        BroadcastSettingChanged(SettingChangeType.BgmVolume);
    }

    public void SetSfxVolume(float value)
    {
        float newValue = Mathf.Clamp01(value);

        if (Mathf.Approximately(setting.sfxVolume, newValue))
            return;

        setting.sfxVolume = newValue;
        ApplyAudio();
        Save();
        BroadcastSettingChanged(SettingChangeType.SfxVolume);
    }

    public void SetCursorScale(float value)
    {
        float newValue = Mathf.Clamp(value, 0.5f, 3f);

        if (Mathf.Approximately(setting.cursorScale, newValue))
            return;

        setting.cursorScale = newValue;
        ApplyCursor();
        Save();
        BroadcastSettingChanged(SettingChangeType.CursorScale);
    }

    public void SetIndicatorSpriteSize(float value)
    {
        int index = Mathf.RoundToInt(value);
        index = Mathf.Clamp(index, 0, 2);

        if (setting.indicatorSpriteSize == index)
            return;

        setting.indicatorSpriteSize = index;
        Save();
        BroadcastSettingChanged(SettingChangeType.IndicatorSpriteSize);
    }

    public IndicatorSpriteSize GetIndicatorSpriteSize()
    {
        int index = Mathf.Clamp(setting.indicatorSpriteSize, 0, 2);
        return (IndicatorSpriteSize)index;
    }

    public void SetShowDamagePopup(bool value)
    {
        if (setting.showDamagePopup == value)
            return;

        setting.showDamagePopup = value;
        Save();
        BroadcastSettingChanged(SettingChangeType.ShowDamagePopup);
    }

    public void SetShowHealthBar(bool value)
    {
        if (setting.showHealthBar == value)
            return;

        setting.showHealthBar = value;
        Save();
        BroadcastSettingChanged(SettingChangeType.ShowHealthBar);
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

    private void BroadcastSettingChanged(SettingChangeType changeType)
    {
        for (int i = settingListeners.Count - 1; i >= 0; i--)
        {
            MonoBehaviour listener = settingListeners[i];

            if (listener == null)
            {
                settingListeners.RemoveAt(i);
                continue;
            }

            if (listener is ISettingChangeListener settingListener)
            {
                settingListener.OnSettingChanged(setting, changeType);
            }
            else
            {
                Debug.LogWarning($"{listener.name}은 ISettingChangeListener를 구현하지 않았습니다.");
            }
        }
    }
}