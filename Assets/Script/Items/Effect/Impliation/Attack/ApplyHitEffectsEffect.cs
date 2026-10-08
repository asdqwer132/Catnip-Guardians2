using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ApplyHitEffectsEffect", menuName = "GameData/Items/Effects/Attack/Apply Hit Effects")]
public sealed class ApplyHitEffectsEffect : ItemEffectData
{
    public TargetSelection selection = new TargetSelection();
    [Tooltip("켜면 EnemyManager에 등록된 활성 적 전체를 대상으로 합니다.")]
    public bool allActiveEnemies;
    public LayerMask enemyLayerMask = ~0;
    public HitEffectData[] effects;
    public override void Prepare(ItemEffectContext context) => HitEffectPreparation.Prepare(effects, context);
    public override void ExecuteEffect(ItemEffectContext context)
    {
        List<Enemy> targets = new List<Enemy>();
        if (allActiveEnemies)
        {
            if (EnemyManager.instance == null) return;
            foreach (Enemy enemy in EnemyManager.instance.ActiveEnemies)
                if (enemy != null && enemy.CanReceiveHitEffects) targets.Add(enemy);
        }
        else
        {
            if (selection == null) return;
            EnemyQueryBuffer query = new EnemyQueryBuffer();
            float radius = selection.range * EffectExecutionScale.Safe(context.rangeMultiplier);
            query.Scan(context.targetPosition, radius, enemyLayerMask);
            TargetSelection scaledSelection = new TargetSelection { mode = selection.mode, range = radius,
                count = selection.count, allowDuplicates = selection.allowDuplicates,
                preferredEnemyClasses = selection.preferredEnemyClasses };
            scaledSelection.SelectMany(query.Enemies, context.targetPosition, targets);
        }
        List<HitEffectContext> snapshots = new List<HitEffectContext>();
        HitEffectAttackState attack = new HitEffectAttackState();
        foreach (Enemy target in targets) snapshots.Add(new HitEffectContext(target, context, attack));
        foreach (HitEffectContext hit in snapshots)
        {
            if (!context.CanContinue) break;
            if (!hit.IsTargetValid) continue;
            foreach (HitEffectData effect in effects ?? System.Array.Empty<HitEffectData>())
                if (context.CanContinue && effect != null) effect.TryExecute(hit);
        }
    }
}
