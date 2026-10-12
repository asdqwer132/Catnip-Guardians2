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
        Check("contact damage stats sanitize functional fields independently of creation stats", () => {
            var stat = new SummonContactDamageStat { interval = float.NaN, damage = -2f, radius = float.PositiveInfinity };
            stat.Clamp(); Eq(stat.interval, 0.5f); Eq(stat.damage, 0f); Eq(stat.radius, 0.3f);
        });
        Check("spawn and module clones leave their original configuration intact", () => {
            var spawn = new SummonSpawnStat { count = 3f, lifetime = 7f };
            var actor = new SummonContactDamageStat { damage = 10f };
            var a = spawn.Clone(); var b = actor.Clone(); a.count = 9f; b.damage = 100f;
            Eq(spawn.count, 3f); Eq(actor.damage, 10f);
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
        Check("module execution scaling leaves damage for the actual attack path", () => {
            var stat = new SummonContactDamageStat { damage = 10f, radius = 4f };
            var context = Context(); context.rangeMultiplier = 2f; context.damageMultiplier = 3f;
            var scaled = EffectExecutionScaling.Apply(stat,context);
            Eq(scaled.radius,8f); Eq(scaled.damage,10f);
        });
        Check("typed creation modifiers change spawn fields without matching contact damage stats", () => {
            var modifier = new FloatFieldBuffModifier { targetStatTypeName = nameof(SummonSpawnStat), fieldName = nameof(SummonSpawnStat.count), addValue = 2f };
            var spawn = new SummonSpawnStat(); var actor = new SummonContactDamageStat();
            True(modifier.CanApplyTo(spawn,null)); True(!modifier.CanApplyTo(actor,null));
            modifier.ApplyTo(spawn,1,null); True(spawn.SpawnCount == 3); Eq(actor.damage,10f);
        });
        Check("typed module modifiers change function fields without matching creation stats", () => {
            var modifier = new FloatFieldBuffModifier { targetStatTypeName = nameof(SummonContactDamageStat), fieldName = nameof(SummonContactDamageStat.damage), multiplyValue = 0.5f };
            var actor = new SummonContactDamageStat { damage = 10f }; var spawn = new SummonSpawnStat();
            True(modifier.CanApplyTo(actor,null)); True(!modifier.CanApplyTo(spawn,null));
            modifier.ApplyTo(actor,1,null); Eq(actor.damage,15f); Eq(spawn.count,1f);
        });
        Check("each module clone keeps its own runtime values", () => {
            var profile = new SummonContactDamageStat { damage = 7f };
            var a = profile.Clone(); var b = profile.Clone(); a.damage = 99f;
            Eq(profile.damage,7f); Eq(b.damage,7f);
        });
        Check("typed contact modifiers do not match turret damage", () => {
            var modifier = new FloatFieldBuffModifier { targetStatTypeName = nameof(SummonContactDamageStat), fieldName = "damage", addValue = 5f };
            True(!modifier.CanApplyTo(new SummonTurretStat(),null));
        });
        Check("orbit and periodic profiles clamp invalid inputs independently", () => {
            var orbit = new SummonOrbitStat { angularSpeed = -9000f, radius = float.NaN };
            orbit.Clamp(); Eq(orbit.angularSpeed,-3600f); Eq(orbit.radius,2f);
            var periodic = new SummonPeriodicSpawnStat { interval = float.NaN, activationCount = -2f };
            periodic.Clamp(); Eq(periodic.interval,1f); Eq(periodic.activationCount,1f);
        });
        Check("turret range and projectile duration scale once", () => {
            var context = Context(); context.rangeMultiplier = 2f; context.durationMultiplier = 3f;
            var profile = new SummonTurretStat { range = 4f, projectileRadius = 0.1f, projectileLifetime = 2f };
            var runtime = EffectExecutionScaling.Apply(profile,context);
            Eq(runtime.range,8f); Eq(runtime.projectileRadius,0.2f); Eq(runtime.projectileLifetime,6f);
            Eq(profile.projectileLifetime,2f);
        });
        RunSummonContactAndScheduleChecks();
    }
}
