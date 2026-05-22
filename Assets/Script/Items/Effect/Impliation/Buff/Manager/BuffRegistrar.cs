using System.Collections.Generic;
using UnityEngine;

public class BuffRegistrar
{
    private BuffStorage storage;
    private BuffStatCalculator calculator;

    public BuffRegistrar(
        BuffStorage storage,
        BuffStatCalculator calculator
    )
    {
        this.storage = storage;
        this.calculator = calculator;
    }

    public void RegisterBuff(BuffEffect effect, ItemEffectContext context)
    {
        if (effect == null)
            return;

        if (context == null)
            return;

        if (effect.bonus == null)
            return;

        if (effect.buffInfo == null)
            return;

        BuffInfo finalBuffInfo = calculator.GetBuffedBuffInfo(
            effect.buffInfo,
            context.sourceItemData,
            context.sourceBag
        );

        if (finalBuffInfo == null)
            return;

        finalBuffInfo.Clamp();

        if (finalBuffInfo.useLimitType == BuffUseLimitType.Time &&
            finalBuffInfo.duration <= 0f)
        {
            return;
        }

        switch (effect.targetScope)
        {
            case BuffTarget.Self:
            case BuffTarget.Item:
                RegisterOrUpdateItemBuff(
                    context.sourceItemData,
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.Bag:
                RegisterOrUpdateBagBuff(
                    context.sourceBag,
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.All:
                RegisterOrUpdateGlobalBuff(
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.EnemyInRange:
                RegisterOrUpdateEnemiesInRange(
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.AllEnemies:
                RegisterOrUpdateAllCurrentEnemies(
                    effect,
                    context,
                    finalBuffInfo
                );
                break;

            case BuffTarget.AllEnemiesIncludingFuture:
                RegisterOrUpdateFutureEnemyBuff(
                    effect,
                    context,
                    finalBuffInfo
                );
                break;
        }
    }

    private void RegisterOrUpdateGlobalBuff(
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        RegisterOrUpdateList(
            storage.globalBuffs,
            effect,
            context,
            finalBuffInfo
        );
    }

    private void RegisterOrUpdateFutureEnemyBuff(
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        RegisterOrUpdateList(
            storage.futureEnemyBuffs,
            effect,
            context,
            finalBuffInfo
        );
    }

    private void RegisterOrUpdateBagBuff(
        EquipmentBag bag,
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        List<ActiveBuff> list = storage.GetOrCreateBagBuffs(bag);

        RegisterOrUpdateList(
            list,
            effect,
            context,
            finalBuffInfo
        );
    }

    private void RegisterOrUpdateItemBuff(
        ItemData itemData,
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        List<ActiveBuff> list = storage.GetOrCreateItemBuffs(itemData);

        RegisterOrUpdateList(
            list,
            effect,
            context,
            finalBuffInfo
        );
    }

    private void RegisterOrUpdateEnemyBuff(
        Enemy enemy,
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        List<ActiveBuff> list = storage.GetOrCreateEnemyBuffs(enemy);

        RegisterOrUpdateList(
            list,
            effect,
            context,
            finalBuffInfo
        );
    }

    private void RegisterOrUpdateEnemiesInRange(
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        EnemyBuffStat enemyBuffStat = effect.GetEnemyBuffStat();

        if (enemyBuffStat == null)
            return;

        Vector3 position = context.targetPosition;
        position.z = 0f;

        float radius = Mathf.Max(0.01f, enemyBuffStat.enemyBuffRadius);

        Collider2D[] hits = Physics2D.OverlapCircleAll(
            position,
            radius,
            enemyBuffStat.enemyTargetLayer
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Enemy enemy = hits[i].GetComponent<Enemy>();

            if (enemy == null)
                enemy = hits[i].GetComponentInParent<Enemy>();

            if (enemy == null)
                continue;

            if (!enemyBuffStat.affectDeadEnemies && enemy.IsDead)
                continue;

            RegisterOrUpdateEnemyBuff(
                enemy,
                effect,
                context,
                finalBuffInfo
            );
        }
    }

    private void RegisterOrUpdateAllCurrentEnemies(
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        EnemyBuffStat enemyBuffStat = effect.GetEnemyBuffStat();

        if (enemyBuffStat == null)
            return;

        for (int i = 0; i < storage.registeredEnemies.Count; i++)
        {
            Enemy enemy = storage.registeredEnemies[i];

            if (enemy == null)
                continue;

            if (!enemyBuffStat.affectDeadEnemies && enemy.IsDead)
                continue;

            RegisterOrUpdateEnemyBuff(
                enemy,
                effect,
                context,
                finalBuffInfo
            );
        }
    }

    private void RegisterOrUpdateList(
        List<ActiveBuff> list,
        BuffEffect effect,
        ItemEffectContext context,
        BuffInfo finalBuffInfo
    )
    {
        if (list == null)
            return;

        ActiveBuff existing = FindSameBuff(
            list,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData
        );

        if (existing != null)
        {
            existing.ApplyRegisterAgain(finalBuffInfo);
            return;
        }

        ActiveBuff activeBuff = new ActiveBuff(
            effect.bonus,
            finalBuffInfo,
            context.sourceItemData,
            context.sourceBag,
            context.currentEffectData,
            effect.includeSelf,
            effect.showInUI
        );

        list.Add(activeBuff);
    }

    private ActiveBuff FindSameBuff(
        List<ActiveBuff> buffs,
        ItemData sourceItemData,
        EquipmentBag sourceBag,
        ItemEffectData sourceEffectData
    )
    {
        if (buffs == null)
            return null;

        for (int i = 0; i < buffs.Count; i++)
        {
            ActiveBuff buff = buffs[i];

            if (buff == null)
                continue;

            if (buff.IsExpired)
                continue;

            if (buff.IsSameBuff(
                    sourceItemData,
                    sourceBag,
                    sourceEffectData
                ))
            {
                return buff;
            }
        }

        return null;
    }
}