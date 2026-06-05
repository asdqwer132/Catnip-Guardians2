using System.Collections.Generic;
using UnityEngine;

public class EnemyStatusManager : MonoBehaviour, ISettingChangeListener
{
    public static EnemyStatusManager instance;

    private readonly List<Enemy> enemies = new List<Enemy>();

    [Header("Runtime Setting")]
    [SerializeField] private bool showHealthBar = true;

    public int EnemyCount => enemies.Count;
    public bool ShowHealthBar => showHealthBar;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        SyncFromSettingManager();
        ApplyHealthBarVisibleToAll();
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
            changeType != SettingChangeType.ShowHealthBar)
            return;

        showHealthBar = setting.showHealthBar;
        ApplyHealthBarVisibleToAll();
    }

    private void SyncFromSettingManager()
    {
        if (SettingManager.instance == null)
            return;

        GameSettingData setting = SettingManager.instance.GetSetting();

        if (setting == null)
            return;

        showHealthBar = setting.showHealthBar;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!enemies.Contains(enemy))
            enemies.Add(enemy);

        enemy.SetHealthBarVisibleBySetting(showHealthBar);
    }

    public void RemoveEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        enemies.Remove(enemy);
    }

    public void ApplyHealthBarVisibleToAll()
    {
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            Enemy enemy = enemies[i];

            if (enemy == null || !enemy.gameObject.activeInHierarchy)
            {
                enemies.RemoveAt(i);
                continue;
            }

            enemy.SetHealthBarVisibleBySetting(showHealthBar);
        }
    }

    public void Clear()
    {
        enemies.Clear();
    }
}