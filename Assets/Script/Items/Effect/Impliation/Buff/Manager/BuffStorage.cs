using System.Collections.Generic;

public class BuffStorage
{
    public readonly List<ActiveBuff> activeBuffs = new List<ActiveBuff>();
    public readonly List<Enemy> registeredEnemies = new List<Enemy>();
    public readonly List<EnemySpawner> registeredEnemySpawners = new List<EnemySpawner>();

    public void AddOrRefresh(ActiveBuff newBuff, BuffInfo info)
    {
        if (newBuff == null)
            return;

        ActiveBuff same = FindSameBuff(newBuff.sourceItemData, newBuff.sourceBag, newBuff.sourceEffectData, newBuff.target);
        if (same != null)
        {
            same.modifiers = newBuff.modifiers;
            same.includeSelf = newBuff.includeSelf;
            same.showInUI = newBuff.showInUI;
            same.RegisterAgain(info);
            return;
        }

        activeBuffs.Add(newBuff);
    }

    public ActiveBuff FindSameBuff(ItemData sourceItemData, EquipmentBag sourceBag, ItemEffectData sourceEffectData, BuffTargetHandle target)
    {
        for (int i = 0; i < activeBuffs.Count; i++)
        {
            ActiveBuff buff = activeBuffs[i];
            if (buff == null || buff.IsExpired)
                continue;

            if (buff.IsSameBuff(sourceItemData, sourceBag, sourceEffectData, target))
                return buff;
        }

        return null;
    }

    public void RegisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        if (!registeredEnemies.Contains(enemy))
            registeredEnemies.Add(enemy);
    }

    public void UnregisterEnemy(Enemy enemy)
    {
        if (enemy == null)
            return;

        registeredEnemies.Remove(enemy);
        RemoveBuffsForEnemy(enemy);
    }

    public void RegisterEnemySpawner(EnemySpawner spawner)
    {
        if (spawner == null)
            return;

        if (!registeredEnemySpawners.Contains(spawner))
            registeredEnemySpawners.Add(spawner);
    }

    public void UnregisterEnemySpawner(EnemySpawner spawner)
    {
        if (spawner == null)
            return;

        registeredEnemySpawners.Remove(spawner);
        RemoveBuffsForEnemySpawner(spawner);
    }

    public void RemoveBuffsForEnemy(Enemy enemy)
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];
            if (buff != null && buff.target != null && buff.target.kind == BuffTargetKind.Enemy && buff.target.enemy == enemy)
                activeBuffs.RemoveAt(i);
        }
    }

    public void RemoveBuffsForEnemySpawner(EnemySpawner spawner)
    {
        for (int i = activeBuffs.Count - 1; i >= 0; i--)
        {
            ActiveBuff buff = activeBuffs[i];
            if (buff != null && buff.target != null && buff.target.kind == BuffTargetKind.EnemySpawner && buff.target.enemySpawner == spawner)
                activeBuffs.RemoveAt(i);
        }
    }

    public void ClearAll()
    {
        activeBuffs.Clear();
    }

    public void RemoveNullRegisters()
    {
        for (int i = registeredEnemies.Count - 1; i >= 0; i--)
        {
            if (registeredEnemies[i] == null)
                registeredEnemies.RemoveAt(i);
        }

        for (int i = registeredEnemySpawners.Count - 1; i >= 0; i--)
        {
            if (registeredEnemySpawners[i] == null)
                registeredEnemySpawners.RemoveAt(i);
        }
    }
}
