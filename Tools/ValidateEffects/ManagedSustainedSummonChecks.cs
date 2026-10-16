using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public static partial class Runner
{
    private sealed class SustainedProbe
    {
        public SummonGroupDefinition group;
        public SummonGroupSustainedSpawnModule module;
        public SummonDefinition member;
        public readonly List<SummonActor> specials = new List<SummonActor>();
        public readonly List<ItemEffectContext> executions = new List<ItemEffectContext>();
    }
    private static SustainedProbe Sustained(List<SummonActor> actors, int required = 2)
    {
        var result = new SustainedProbe { group = new SummonGroupDefinition() };
        result.member = new SummonDefinition { summonGroup = result.group };
        var spawn = new SummonSpawnEffect { onExecute = context => {
            result.executions.Add(context);
            // Deliberately use the same group: the maintained actor must not satisfy its own condition.
            var actor = GroupActor(actors, result.member, context.owner, notify: false);
            actor.transform.position = context.targetPosition;
            actor.execution = context.Copy(context.targetPosition, context.direction);
            result.specials.Add(actor);
            SummonGroupRuntime.NotifyReady(actor);
            context.onSummonSpawned?.Invoke(actor);
        } };
        result.module = new SummonGroupSustainedSpawnModule {
            condition = new SummonGroupPopulationCondition { requirementMode = SummonGroupRequirementMode.TotalInstances, requiredInstances = required },
            spawnEffects = new[] { spawn }
        };
        result.group.reactionModules = new SummonGroupReactionModule[] { result.module };
        return result;
    }
    private static void RunSustainedSummonChecks()
    {
        GroupCheck("maintained specials spawn once while filled and disappear immediately when the condition breaks", actors => {
            var probe = Sustained(actors); GroupActor(actors, probe.member);
            True(probe.specials.Count == 0);
            var last = GroupActor(actors, probe.member); True(probe.specials.Count == 1);
            GroupActor(actors, probe.member); True(probe.specials.Count == 1);
            last.Despawn(false); True(probe.specials[0].CanAct);
            actors[0].Despawn(false); True(!probe.specials[0].CanAct);
            GroupActor(actors, probe.member); True(probe.specials.Count == 2 && probe.specials[1].CanAct);
        });
        GroupCheck("specials and their context copies cannot sustain their own population or trigger item reactions", actors => {
            var probe = Sustained(actors, 1); var parent = GroupActor(actors, probe.member);
            var execution = probe.executions[0];
            True(!execution.countsForSummonGroupPopulation && !execution.allowSummonGroupReactions && !execution.allowItemUseReactions);
            True(!execution.consumeUseBuffs && SummonGroupRuntime.GetPopulation(probe.group, null).instanceCount == 1);
            var inherited = new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null);
            inherited.InheritExecution(execution); True(!inherited.countsForSummonGroupPopulation);
            parent.Despawn(false); True(!probe.specials[0].CanAct && execution.lifetime.IsCancelled);
        });
        GroupCheck("exact quotas can activate on a member leaving and remove specials on a member entering", actors => {
            var probe = Sustained(actors); probe.module.condition.comparison = SummonGroupCountComparison.Exactly;
            GroupActor(actors, probe.member, notify: false); GroupActor(actors, probe.member, notify: false);
            var third = GroupActor(actors, probe.member); True(probe.specials.Count == 0);
            third.Despawn(false); True(probe.specials.Count == 1);
            GroupActor(actors, probe.member); True(!probe.specials[0].CanAct);
        });
        GroupCheck("the last spawn point is recorded rather than following a moved triggering summon", actors => {
            var probe = Sustained(actors, 1);
            var actor = GroupActor(actors, probe.member, notify: false);
            actor.transform.position = new Vector3(4, 7, 0); SummonGroupRuntime.NotifyReady(actor);
            Eq(probe.specials[0].transform.position.x, 4); Eq(probe.specials[0].transform.position.y, 7);
            actor.transform.position = new Vector3(99, 99, 0);
            probe.specials[0].Despawn(true); SummonGroupRuntime.TickForFrame(10, 1f);
            True(probe.specials.Count == 2); Eq(probe.specials[1].transform.position.x, 4);
        });
        GroupCheck("a cancelled last source does not cancel a still-satisfied group", actors => {
            var probe = Sustained(actors); GroupActor(actors, probe.member); GroupActor(actors, probe.member);
            var last = GroupActor(actors, probe.member, notify: false);
            last.execution.lifetime = new ItemEffectLifetime(); SummonGroupRuntime.NotifyReady(last);
            last.execution.lifetime.Close(false); last.Despawn(false);
            True(probe.specials[0].CanAct);
            probe.specials[0].Despawn(true); SummonGroupRuntime.TickForFrame(11, 1f);
            True(probe.specials.Count == 2 && probe.specials[1].CanAct);
        });
        GroupCheck("maintained group runtimes are isolated by owner and can share a module asset", actors => {
            var probe = Sustained(actors, 1); var a = new GameObject(); var b = new GameObject();
            var first = GroupActor(actors, probe.member, a); GroupActor(actors, probe.member, b);
            True(probe.specials.Count == 2 && ReferenceEquals(probe.specials[1].Owner, b));
            first.Despawn(false); True(!probe.specials[0].CanAct && probe.specials[1].CanAct);
        });
        GroupCheck("disabling replenishment leaves expired specials absent until the condition is reached again", actors => {
            var probe = Sustained(actors, 1); probe.module.respawnWhenAllGone = false;
            var parent = GroupActor(actors, probe.member); probe.specials[0].Despawn(true);
            SummonGroupRuntime.TickForFrame(12, 1f); True(probe.specials.Count == 1);
            parent.Despawn(false); GroupActor(actors, probe.member); True(probe.specials.Count == 2);
        });
        GroupCheck("keep-existing option stops new creation without cancelling the previous actors", actors => {
            var probe = Sustained(actors, 1); probe.module.despawnWhenConditionLost = false;
            var parent = GroupActor(actors, probe.member); parent.Despawn(false);
            True(probe.specials[0].CanAct && !probe.executions[0].lifetime.IsCancelled);
            SummonGroupRuntime.TickForFrame(13, 1f); True(probe.specials.Count == 1);
        });
        GroupCheck("pooled actors with a new LifeId are not recalled by the previous condition", actors => {
            var probe = Sustained(actors, 1); var parent = GroupActor(actors, probe.member);
            var reused = probe.specials[0]; reused.LifeId++; reused.execution = new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null);
            reused.Definition = null; parent.Despawn(false); True(reused.CanAct);
        });
        GroupCheck("removing a reaction module or resetting runtime recalls only its own actors", actors => {
            var probe = Sustained(actors, 1); var parent = GroupActor(actors, probe.member);
            probe.group.reactionModules = null; SummonGroupRuntime.NotifyFieldChanged();
            True(!probe.specials[0].CanAct && parent.CanAct);
            probe.group.reactionModules = new SummonGroupReactionModule[] { probe.module, probe.module };
            SummonGroupRuntime.NotifyFieldChanged(); True(probe.specials.Count == 2);
            SummonGroupRuntime.Reset(); True(!probe.specials[1].CanAct && parent.CanAct);
        });
        GroupCheck("frame guard prevents one replenishment tick per source actor", actors => {
            var probe = Sustained(actors, 1); GroupActor(actors, probe.member);
            probe.specials[0].Despawn(true); SummonGroupRuntime.TickForFrame(14, .1f);
            SummonGroupRuntime.TickForFrame(14, 1f); True(probe.specials.Count == 1);
            SummonGroupRuntime.TickForFrame(15, .2f); True(probe.specials.Count == 2);
        });
        GroupCheck("a field change during special creation is reevaluated without retaining an invalid special", actors => {
            var probe = Sustained(actors); var first = GroupActor(actors, probe.member);
            var spawn = probe.module.spawnEffects[0]; var original = spawn.onExecute;
            spawn.onExecute = context => { first.Despawn(false); original(context); };
            GroupActor(actors, probe.member);
            True(probe.specials.Count == 1 && !probe.specials[0].CanAct);
            True(SummonGroupRuntime.GetPopulation(probe.group, null).instanceCount == 1);
        });
        GroupCheck("combat generation cancellation recalls tracked specials and new combat can build its own scope", actors => {
            var probe = Sustained(actors, 1); GroupActor(actors, probe.member);
            ItemEffectRuntime.CancelAll(); SummonGroupRuntime.TickForFrame(16, .1f);
            True(!probe.specials[0].CanAct && !SummonRegistry.Active.Contains(probe.specials[0]));
            GroupActor(actors, probe.member); True(probe.specials.Count == 2 && probe.specials[1].CanAct);
        });
        Check("spawn position supports fixed and owner offsets and random points inside the configured disk", () => {
            var owner = new GameObject(); owner.transform.position = new Vector3(3, 4, 0);
            var context = new SummonGroupReactionContext { source = new ItemEffectContext(owner, null, Vector3.zero, Vector3.zero, null), lastSummonPosition = new Vector3(8, 9, 0) };
            var position = new SummonGroupSpawnPosition { mode = SummonGroupSpawnPositionMode.OwnerPosition, offset = new Vector3(1, 2, 5) };
            True(position.TryResolve(context, out var point)); Eq(point.x, 4); Eq(point.y, 6); Eq(point.z, 0);
            position.mode = SummonGroupSpawnPositionMode.FixedWorldPosition; position.fixedWorldPosition = new Vector3(-5, -7, 0);
            True(position.TryResolve(context, out point)); Eq(point.x, -4); Eq(point.y, -5);
            position.mode = SummonGroupSpawnPositionMode.RandomPosition; position.randomRadius = 3; position.randomCenter = SummonGroupRandomCenter.FixedWorldPosition;
            for (int i = 0; i < 200; i++) { True(position.TryResolve(context, out point)); True((point - new Vector3(-4, -5, 0)).sqrMagnitude <= 9.0001f); }
            position.randomRadius = float.NaN; True(position.TryResolve(context, out point) && !float.IsNaN(point.x));
        });
        Check("registered player positions follow movement and are removed when the anchor disables", () => {
            var context = new SummonGroupReactionContext { source = new ItemEffectContext(null, null, Vector3.zero, Vector3.zero, null) };
            var position = new SummonGroupSpawnPosition { mode = SummonGroupSpawnPositionMode.PlayerPosition };
            True(!position.TryResolve(context, out _));
            var host = new GameObject(); var anchor = new EffectPositionAnchor { gameObject = host, transform = host.transform };
            typeof(EffectPositionAnchor).GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(anchor, null);
            try {
                host.transform.position = new Vector3(10, 20, 0); True(position.TryResolve(context, out var point)); Eq(point.x, 10);
                host.transform.position = new Vector3(30, 40, 0); True(position.TryResolve(context, out point)); Eq(point.y, 40);
            } finally { typeof(EffectPositionAnchor).GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(anchor, null); }
            True(!position.TryResolve(context, out _));
        });
    }
}
