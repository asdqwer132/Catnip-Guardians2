using System;
using UnityEngine;

public static partial class Runner
{
    private static void RunSummonChecks()
    {
        Check("spawn stat count floors fractions and supports zero without overflowing", () => {
            foreach (var entry in new[] { (0f,0), (0.99f,0), (1.99f,1), (3f,3), (float.MaxValue,128) })
                True(new SummonSpawnStat { count = entry.Item1 }.SpawnCount == entry.Item2);
        });
        Check("spawn count and lifetime sanitize nonfinite and negative input", () => {
            var stat = new SummonSpawnStat { count = float.NaN, lifetime = -1f, spreadRadius = float.PositiveInfinity };
            stat.Clamp(); True(stat.SpawnCount == 1); Eq(stat.lifetime, 0.01f); Eq(stat.spreadRadius, 0f);
        });
        Check("actor stats sanitize functional fields independently of creation stats", () => {
            var stat = new SummonActorStat { attackInterval = float.NaN, attackPower = -2f, detectionRange = float.PositiveInfinity };
            stat.Clamp(); Eq(stat.attackInterval, 0.5f); Eq(stat.attackPower, 0f); Eq(stat.detectionRange, 0.5f);
        });
        Check("spawn and actor clones leave their original configuration intact", () => {
            var spawn = new SummonSpawnStat { count = 3f, lifetime = 7f };
            var actor = new SummonActorStat { attackPower = 10f };
            var a = spawn.Clone(); var b = actor.Clone(); a.count = 9f; b.attackPower = 100f;
            Eq(spawn.count, 3f); Eq(actor.attackPower, 10f);
        });
        Check("even full circle creates distinct 2D positions around the target", () => {
            var stat = new SummonSpawnStat { count = 4f, spreadRadius = 2f };
            var origin = new Vector3(10f,20f,9f);
            var expected = new[] { new Vector2(12f,20f), new Vector2(10f,22f), new Vector2(8f,20f), new Vector2(10f,18f) };
            for (int i=0; i<4; i++) {
                Vector3 p = stat.GetPosition(AttackPlacementMode.CircleEven, origin, Vector3.right, i, 4, AttackSpreadStartMode.FromDirection);
                Eq(p.x,expected[i].x); Eq(p.y,expected[i].y); Eq(p.z,0f);
            }
        });
        Check("zero spread radius preserves the target for every spawn", () => {
            var stat = new SummonSpawnStat { spreadRadius = 0f };
            for (int i=0; i<5; i++) {
                var p = stat.GetPosition(AttackPlacementMode.CircleEven,new Vector3(2f,3f,8f),Vector3.right,i,5);
                Eq(p.x,2f); Eq(p.y,3f); Eq(p.z,0f);
            }
        });
        Check("forward layout uses buffable forward and side spacing", () => {
            var stat = new SummonSpawnStat { forwardSpacing = 2f, sideSpacing = 1f };
            var p = stat.GetPosition(AttackPlacementMode.Forward,Vector3.zero,Vector3.right,2,3);
            Eq(p.x,4f); Eq(p.y,-2f);
        });
        Check("random disk layout stays inside the configured radius", () => {
            var stat = new SummonSpawnStat { spreadRadius = 3f };
            for (int i=0;i<100;i++)
                True(stat.GetPosition(AttackPlacementMode.CircleRandom,Vector3.zero,Vector3.right,i,100).sqrMagnitude <= 9.0001f);
        });
        Check("empty planar direction falls back to world right", () => {
            var stat = new SummonSpawnStat { forwardSpacing = 2f };
            var p = stat.GetPosition(AttackPlacementMode.Forward,Vector3.zero,new Vector3(0f,0f,99f),1,2);
            Eq(p.x,2f); Eq(p.y,0f); Eq(p.z,0f);
        });
        Check("clockwise layout reverses the spread without changing distance", () => {
            var stat = new SummonSpawnStat { spreadRadius = 2f, spreadAngle = 180f };
            var a = stat.GetPosition(AttackPlacementMode.CircleEven,Vector3.zero,Vector3.right,1,3,AttackSpreadStartMode.FromDirection);
            var b = stat.GetPosition(AttackPlacementMode.CircleEven,Vector3.zero,Vector3.right,1,3,AttackSpreadStartMode.FromDirection,true);
            Eq(a.y,2f); Eq(b.y,-2f); Eq(a.magnitude,b.magnitude);
        });
        Check("creation execution scaling affects lifetime and layout but keeps count", () => {
            var stat = new SummonSpawnStat { lifetime = 5f, count = 3f, spreadRadius = 2f, forwardSpacing = 1f };
            var context = Context(); context.durationMultiplier = 2f; context.rangeMultiplier = 3f;
            var scaled = EffectExecutionScaling.Apply(stat,context);
            Eq(scaled.lifetime,10f); Eq(scaled.spreadRadius,6f); Eq(scaled.forwardSpacing,3f); Eq(scaled.count,3f);
            Eq(stat.lifetime,5f); Eq(stat.spreadRadius,2f);
        });
        Check("actor execution scaling leaves damage for the actual attack path", () => {
            var stat = new SummonActorStat { attackPower = 10f, detectionRange = 4f, damageMultiplier = 1.5f };
            var context = Context(); context.rangeMultiplier = 2f; context.damageMultiplier = 3f;
            var scaled = EffectExecutionScaling.Apply(stat,context);
            Eq(scaled.detectionRange,8f); Eq(scaled.attackPower,10f); Eq(scaled.damageMultiplier,1.5f);
        });
        Check("typed creation modifiers change spawn fields without matching actor stats", () => {
            var modifier = new FloatFieldBuffModifier { targetStatTypeName = nameof(SummonSpawnStat), fieldName = nameof(SummonSpawnStat.count), addValue = 2f };
            var spawn = new SummonSpawnStat(); var actor = new SummonActorStat();
            True(modifier.CanApplyTo(spawn,null)); True(!modifier.CanApplyTo(actor,null));
            modifier.ApplyTo(spawn,1,null); True(spawn.SpawnCount == 3); Eq(actor.attackPower,0f);
        });
        Check("typed actor modifiers change function fields without matching creation stats", () => {
            var modifier = new FloatFieldBuffModifier { targetStatTypeName = nameof(SummonActorStat), fieldName = nameof(SummonActorStat.attackPower), multiplyValue = 0.5f };
            var actor = new SummonActorStat { attackPower = 10f }; var spawn = new SummonSpawnStat();
            True(modifier.CanApplyTo(actor,null)); True(!modifier.CanApplyTo(spawn,null));
            modifier.ApplyTo(actor,1,null); Eq(actor.attackPower,15f); Eq(spawn.count,1f);
        });
        Check("owned actor cache is separate from the effect creation snapshot", () => {
            var context = Context(); var key = new ItemEffectData();
            var spawn = context.plan.GetSnapshot(key,new SummonSpawnStat { count = 2f },context);
            var actor = context.plan.GetOwnedSnapshot(key,new SummonActorStat { attackPower = 7f },context);
            Eq(spawn.count,2f); Eq(actor.attackPower,7f);
        });
        Check("same owner shares one captured stat and different owners keep their own settings", () => {
            var context = Context(); var owner = new object(); var stat = new SummonActorStat { attackPower = 7f };
            var first = context.plan.GetOwnedSnapshot(owner,stat,context); stat.attackPower = 99f;
            True(ReferenceEquals(first,context.plan.GetOwnedSnapshot(owner,stat,context)));
            Eq(first.attackPower,7f);
            Eq(context.plan.GetOwnedSnapshot(new object(),stat,context).attackPower,99f);
        });
        Check("owned actor snapshots produce independent per-instance copies", () => {
            var context = Context(); var snapshot = context.plan.GetOwnedSnapshot(new object(),new SummonActorStat { attackPower = 7f },context);
            var a = BuffStatCloneUtility.Clone(snapshot); var b = BuffStatCloneUtility.Clone(snapshot); a.attackPower = 99f;
            Eq(snapshot.attackPower,7f); Eq(b.attackPower,7f);
        });
        Check("owned actor capture and clones retain universal application history", () => {
            var buff = GlobalRecord(new BuffStorage());
            var baseStat = new SummonActorStat { attackPower = 7.5f }; BuffStatCloneUtility.RecordApplied(baseStat,buff);
            var context = Context(); var snapshot = context.plan.GetOwnedSnapshot(new object(),baseStat,context);
            True(BuffStatCloneUtility.WasApplied(snapshot,buff));
            True(BuffStatCloneUtility.WasApplied(BuffStatCloneUtility.Clone(snapshot),buff));
        });
    }
}
