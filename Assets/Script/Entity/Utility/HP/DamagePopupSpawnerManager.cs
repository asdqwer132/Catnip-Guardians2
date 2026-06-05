using System.Collections.Generic;
using UnityEngine;

public class DamagePopupSpawnerManager : MonoBehaviour, ISettingChangeListener
{
    public static DamagePopupSpawnerManager instance;

    private readonly List<DamagePopup> activePopups = new List<DamagePopup>();

    [Header("Runtime Setting")]
    [SerializeField] private bool showDamagePopup = true;

    public IReadOnlyList<DamagePopup> ActivePopups => activePopups;
    public int ActivePopupCount => activePopups.Count;
    public bool ShowDamagePopup => showDamagePopup;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public void OnSettingChanged(GameSettingData setting, SettingChangeType changeType)
    {
        if (setting == null)
            return;

        if (changeType != SettingChangeType.All &&
            changeType != SettingChangeType.ShowDamagePopup)
            return;

        showDamagePopup = setting.showDamagePopup;

        if (!showDamagePopup)
            ClearAllPopups();
    }

    public bool CanShowDamagePopup()
    {
        return showDamagePopup;
    }

    public void RegisterPopup(DamagePopup popup)
    {
        if (popup == null)
            return;

        if (activePopups.Contains(popup))
            return;

        activePopups.Add(popup);
    }

    public void UnregisterPopup(DamagePopup popup)
    {
        if (popup == null)
            return;

        activePopups.Remove(popup);
    }

    public void ClearAllPopups()
    {
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            if (activePopups[i] != null)
                Destroy(activePopups[i].gameObject);
        }

        activePopups.Clear();
    }

    public void RemoveNullPopups()
    {
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            if (activePopups[i] == null)
                activePopups.RemoveAt(i);
        }
    }
}