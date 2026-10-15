using UnityEngine;
using UnityEngine.SceneManagement;

public class SettingInjecter : MonoBehaviour, ISettingChangeListener
{
    public static SettingInjecter instance;

    [Header("Camera")]
    public Camera targetCamera;
    private bool followMainCamera;

    [Header("Layer Names")]
    public string damagePopupLayerName = "DamagePopup";
    public string hpBarLayerName = "HpBar";

    private void Awake()
    {
        instance = this;
        followMainCamera = targetCamera == null;

        if (targetCamera == null)
            targetCamera = Camera.main;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.activeSceneChanged += OnActiveSceneChanged;
        if (SettingManager.instance != null)
            SettingManager.instance.AddListener(this);
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        if (SettingManager.instance != null)
            SettingManager.instance.RemoveListener(this);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) => RefreshCamera();
    private void OnActiveSceneChanged(Scene previous, Scene next) => RefreshCamera();
    private void RefreshCamera()
    {
        if (followMainCamera) targetCamera = Camera.main;
        if (SettingManager.instance != null) ApplyAll(SettingManager.instance.GetSetting());
    }

    public void OnSettingChanged(GameSettingData setting, SettingChangeType changeType)
    {
        if (setting == null)
            return;

        if (changeType == SettingChangeType.All)
        {
            ApplyAll(setting);
            return;
        }

        if (changeType == SettingChangeType.ShowDamagePopup)
        {
            SetLayerVisible(damagePopupLayerName, setting.showDamagePopup);
            return;
        }

        if (changeType == SettingChangeType.ShowHealthBar)
        {
            SetLayerVisible(hpBarLayerName, setting.showHealthBar);
            return;
        }
    }

    private void ApplyAll(GameSettingData setting)
    {
        SetLayerVisible(damagePopupLayerName, setting.showDamagePopup);
        SetLayerVisible(hpBarLayerName, setting.showHealthBar);
    }

    private void SetLayerVisible(string layerName, bool visible)
    {
        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null)
            return;

        int layer = LayerMask.NameToLayer(layerName);

        if (layer == -1)
        {
            Debug.LogWarning($"존재하지 않는 레이어입니다: {layerName}");
            return;
        }

        int layerMask = 1 << layer;

        if (visible)
            targetCamera.cullingMask |= layerMask;
        else
            targetCamera.cullingMask &= ~layerMask;
    }
}
