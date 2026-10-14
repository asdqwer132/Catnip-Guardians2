using System;
using System.Collections.Generic;
using UnityEngine;

public static partial class Runner
{
    private static void GroupCheck(string name, Action<List<SummonActor>> action)
        => Check(name, () => {
            SummonGroupRuntime.Reset(); var actors = new List<SummonActor>();
            try { action(actors); }
            finally { foreach (var actor in actors) SummonRegistry.Unregister(actor); SummonGroupRuntime.Reset(); }
        });
    private static SummonGroupDefinition GroupWith(CompletionProbe reward, SummonGroupRequirementMode mode = SummonGroupRequirementMode.Both, int kinds = 3, int instances = 3)
        => new SummonGroupDefinition { itemRules = new[] { new SummonGroupItemRule {
            requirementMode = mode, requiredKinds = kinds, requiredInstances = instances, item = CompletionItem(reward)
        } } };
    private static SummonActor GroupActor(List<SummonActor> actors, SummonDefinition definition, GameObject owner = null,
        bool notify = true, bool reactions = true)
    {
        var host = new GameObject(); var actor = new SummonActor { gameObject = host, transform = host.transform,
            Definition = definition, Owner = owner, execution = new ItemEffectContext(owner,new ItemData(),Vector3.zero,Vector3.zero,null) {
                allowSummonGroupReactions = reactions } };
        actors.Add(actor); SummonRegistry.Register(actor); if (notify) SummonGroupRuntime.NotifyReady(actor); return actor;
    }
    private static void RunSummonGroupChecks()
    {
        GroupCheck("group counts one definition as one kind while counting every live instance", actors => {
            var group=GroupWith(new CompletionProbe());var red=new SummonDefinition{summonGroup=group};
            GroupActor(actors,red);GroupActor(actors,red);GroupActor(actors,red);
            var count=SummonGroupRuntime.GetPopulation(group,null);True(count.kindCount==1&&count.instanceCount==3);
        });
        GroupCheck("both group thresholds fire on the third distinct kind once and rearm after a kind leaves", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward);var red=new SummonDefinition{summonGroup=group};
            var blue=new SummonDefinition{summonGroup=group};var green=new SummonDefinition{summonGroup=group};
            GroupActor(actors,red);GroupActor(actors,red);GroupActor(actors,blue);True(reward.calls.Count==0);
            var third=GroupActor(actors,green);True(reward.calls.Count==1);GroupActor(actors,red);True(reward.calls.Count==1);
            third.active=false;SummonRegistry.Unregister(third);SummonGroupRuntime.NotifyFieldChanged();
            GroupActor(actors,green);True(reward.calls.Count==2);
        });
        GroupCheck("kind-only and instance-only rules can be selected independently", actors => {
            var kindsReward=new CompletionProbe();var countReward=new CompletionProbe();var group=GroupWith(kindsReward,SummonGroupRequirementMode.DistinctKinds,2,99);
            group.itemRules=new[]{group.itemRules[0],new SummonGroupItemRule{requirementMode=SummonGroupRequirementMode.TotalInstances,requiredInstances=3,item=CompletionItem(countReward)}};
            var red=new SummonDefinition{summonGroup=group};GroupActor(actors,red);GroupActor(actors,red);GroupActor(actors,red);
            True(countReward.calls.Count==1&&kindsReward.calls.Count==0);GroupActor(actors,new SummonDefinition{summonGroup=group});True(kindsReward.calls.Count==1);
        });
        GroupCheck("group populations are isolated by owner and exclude other groups inactive and unready summons", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward,SummonGroupRequirementMode.TotalInstances,1,2);
            var red=new SummonDefinition{summonGroup=group};var a=new GameObject();var b=new GameObject();
            GroupActor(actors,red,a);GroupActor(actors,red,b);GroupActor(actors,new SummonDefinition(),a);
            var inactive=GroupActor(actors,red,a,notify:false);inactive.active=false;
            var unready=GroupActor(actors,red,a,notify:false);unready.IsGroupReady=false;
            True(SummonGroupRuntime.GetPopulation(group,a).instanceCount==1&&reward.calls.Count==0);
            GroupActor(actors,red,a);True(reward.calls.Count==1&&ReferenceEquals(reward.calls[0].owner,a));
        });
        GroupCheck("all-owner groups combine the field but award to the owner of the triggering summon", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward,SummonGroupRequirementMode.TotalInstances,1,2);group.ownerScope=SummonOwnerScope.AllOwners;
            var red=new SummonDefinition{summonGroup=group};var a=new GameObject();var b=new GameObject();GroupActor(actors,red,a);GroupActor(actors,red,b);
            True(reward.calls.Count==1&&ReferenceEquals(reward.calls[0].owner,b));GroupActor(actors,red,a);True(reward.calls.Count==1);
        });
        GroupCheck("reward-spawned members count without recursively awarding more items", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward,SummonGroupRequirementMode.TotalInstances,1,1);var red=new SummonDefinition{summonGroup=group};
            reward.onExecute=c=>{True(!c.allowSummonGroupReactions&&!c.allowItemUseReactions);GroupActor(actors,red,c.owner,reactions:c.allowSummonGroupReactions);};
            GroupActor(actors,red);True(reward.calls.Count==1&&SummonGroupRuntime.GetPopulation(group,null).instanceCount==2);
        });
        GroupCheck("failed group item use can retry on a later summon while conditions remain met", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward,SummonGroupRequirementMode.TotalInstances,1,1);var rule=group.itemRules[0];
            var red=new SummonDefinition{summonGroup=group};rule.item=null;GroupActor(actors,red);rule.item=CompletionItem(reward);GroupActor(actors,red);
            True(reward.calls.Count==1);
        });
        GroupCheck("every-matching-summon mode still suppresses reward descendants", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward,SummonGroupRequirementMode.TotalInstances,1,1);
            group.itemRules[0].triggerMode=SummonGroupTriggerMode.EveryMatchingSummon;var red=new SummonDefinition{summonGroup=group};
            GroupActor(actors,red);GroupActor(actors,red);GroupActor(actors,red,reactions:false);True(reward.calls.Count==2);
        });
        GroupCheck("multiple group rules execute independently without repeating a duplicate rule reference", actors => {
            var first=new CompletionProbe();var second=new CompletionProbe();var group=GroupWith(first,SummonGroupRequirementMode.TotalInstances,1,1);
            var rule=group.itemRules[0];group.itemRules=new[]{rule,rule,null,new SummonGroupItemRule{requiredKinds=1,requiredInstances=1,item=CompletionItem(second)}};
            GroupActor(actors,new SummonDefinition{summonGroup=group});True(first.calls.Count==1&&second.calls.Count==1);
        });
        GroupCheck("an actor reinitialized by a reward cannot keep executing rules from its previous life", actors => {
            var first=new CompletionProbe();var second=new CompletionProbe();var group=GroupWith(first,SummonGroupRequirementMode.TotalInstances,1,1);
            group.itemRules=new[]{group.itemRules[0],new SummonGroupItemRule{requiredKinds=1,requiredInstances=1,item=CompletionItem(second)}};
            first.onExecute=c=>c.sourceSummon.LifeId++;GroupActor(actors,new SummonDefinition{summonGroup=group});True(first.calls.Count==1&&second.calls.Count==0);
        });
        GroupCheck("thrown group rewards route the item motion and suppression into the flight", actors => {
            var group=GroupWith(new CompletionProbe(),SummonGroupRequirementMode.TotalInstances,1,1);var delivery=group.itemRules[0].itemUse;
            delivery.mode=ItemUseDeliveryMode.Throw;delivery.throwOptions.overrideMotion=true;delivery.throwOptions.motion.arriveTime=0.25f;
            var actor=GroupActor(actors,new SummonDefinition{summonGroup=group});var flight=actor.executor.flights[0];
            True(ReferenceEquals(actor.executor.lastOptions,delivery.throwOptions)&&ReferenceEquals(flight.sourceItemData,group.itemRules[0].item));
            True(!flight.allowSummonGroupReactions&&!flight.allowItemUseReactions&&!flight.consumeUseBuffs);
        });
        GroupCheck("changing a runtime definition rearms a lost group composition", actors => {
            var reward=new CompletionProbe();var group=GroupWith(reward,SummonGroupRequirementMode.DistinctKinds,2,99);
            var red=new SummonDefinition{summonGroup=group};var blue=new SummonDefinition{summonGroup=group};GroupActor(actors,red);
            var actor=GroupActor(actors,blue);True(reward.calls.Count==1);actor.Definition=red;SummonGroupRuntime.NotifyFieldChanged();
            GroupActor(actors,blue);True(reward.calls.Count==2);
        });
        Check("group comparison supports filled or exact quotas and does not count extra kinds as extra instances", () => {
            var rule=new SummonGroupItemRule{requiredKinds=3,requiredInstances=5};
            True(!rule.Matches(new SummonGroupPopulation{kindCount=3,instanceCount=4}));
            True(rule.Matches(new SummonGroupPopulation{kindCount=3,instanceCount=6}));rule.comparison=SummonGroupCountComparison.Exactly;
            True(!rule.Matches(new SummonGroupPopulation{kindCount=3,instanceCount=6}));True(rule.Matches(new SummonGroupPopulation{kindCount=3,instanceCount=5}));
        });
        Check("group gate reserves pending awards rearms after failure and only repeats after a mismatch", () => {
            var gate=new SummonGroupActivationGate();True(gate.TryBegin(true,SummonGroupTriggerMode.OnConditionReached));
            True(!gate.TryBegin(true,SummonGroupTriggerMode.EveryMatchingSummon));gate.Complete(false);
            True(gate.TryBegin(true,SummonGroupTriggerMode.OnConditionReached));gate.Complete(true);True(!gate.TryBegin(true,SummonGroupTriggerMode.OnConditionReached));
            gate.Observe(false);True(gate.TryBegin(true,SummonGroupTriggerMode.OnConditionReached));gate.Observe(false);gate.Complete(true);
            True(gate.TryBegin(true,SummonGroupTriggerMode.OnConditionReached));gate.Complete(true);
        });
    }
}
