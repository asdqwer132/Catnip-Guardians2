using System;
using UnityEngine;

public static partial class Runner
{
    private static UseItemStatusReactionModule RepeatModule(float chance = 1f)
        => new UseItemStatusReactionModule { allowedSeries = new[] { ItemSeries.Mineral }, probability = chance };
    private static ActiveBuff ReactionStatus(BuffStorage storage, SpecialStatusDefinition key)
        => storage.RegisterSpecialStatus(key, new BuffInfo { useLimitType = BuffUseLimitType.Infinite },
            null, null, new ItemEffectData(), BuffTargetHandle.Item(new ItemData()), true);
    private static ItemEffectContext ReactionSource(CompletionProbe effect, BuffManager manager = null)
        => new ItemEffectContext(new GameObject(), new ItemData { dataId = "MineralTest", series = ItemSeries.Mineral,
                effectDatas = new ItemEffectData[] { effect } }, new Vector3(2,3,0), new Vector3(5,6,0),
            new EquipmentBag(), buffManager: manager, direction: Vector3.right);
    private static ExecuteEffectsStatusReactionModule ExtraEffects(params ItemEffectData[] effects)
        => new ExecuteEffectsStatusReactionModule { effects = effects };

    private static void RunStatusReactionChecks()
    {
        Check("state throw anchors and offsets resolve independently with directional space", () => {
            var context=ReactionSource(new CompletionProbe());context.owner.transform.position=new Vector3(20,30,0);
            context.direction=Vector3.up;var module=RepeatModule();module.throwStartAnchor=StatusReactionThrowAnchor.OwnerPosition;
            module.throwTargetAnchor=StatusReactionThrowAnchor.OriginalUsePosition;module.throwOffsetSpace=StatusReactionOffsetSpace.ForwardAndSide;
            module.throwStartOffset=new Vector3(2,3,0);module.throwTargetOffset=new Vector3(4,5,0);Vector3 start,target;
            True(module.TryGetThrowPath(context,out start,out target));Eq(start.x,23);Eq(start.y,32);Eq(target.x,7);Eq(target.y,7);
        });
        Check("state forward placement reuses direction and forward-side offsets", () => {
            var context=ReactionSource(new CompletionProbe());var module=RepeatModule();module.targetPlacement=AttackPlacementMode.Forward;
            module.directionMode=AttackDirectionMode.FixedWorldDirection;module.fixedWorldDirection=Vector2.up;
            module.forwardOffset=4;module.sideOffset=2;Vector3 start,target;
            True(module.TryGetThrowPath(context,out start,out target));Eq(target.x,7);Eq(target.y,10);
        });
        Check("state shotgun placement uses the existing angular and distance distribution", () => {
            var context=ReactionSource(new CompletionProbe());var module=RepeatModule();module.targetPlacement=AttackPlacementMode.Shotgun;
            module.radius=4;module.shotgunRadiusOffset=1;module.spreadAngle=30;Vector3 start,target;
            for(int i=0;i<20;i++){True(module.TryGetThrowPath(context,out start,out target));var offset=target-context.targetPosition;
                True(offset.magnitude>=2.9999f&&offset.magnitude<=5.0001f&&Vector3.Angle(Vector3.right,offset)<=15.0001f);}
        });
        Check("throw preview does not sample placement randomness and used-item options reach the executor", () => {
            var context=ReactionSource(new CompletionProbe());var module=RepeatModule();module.delivery=StatusReactionItemDelivery.ThrowAtImpact;
            var executor=new ItemThrowExecutor{itemEffectExecutor=new ItemEffectExecutor()};module.targetPlacement=AttackPlacementMode.CircleRandom;
            int before=UnityEngine.Random.DrawCount;True(module.CanReact(context,executor));True(UnityEngine.Random.DrawCount==before);
            True(module.TryReact(context,executor));True(UnityEngine.Random.DrawCount==before+2);True(ReferenceEquals(executor.lastOptions,module.throwOptions));
            True(ReferenceEquals(executor.flights[0].sourceItemData,context.sourceItemData));
        });
        Check("module motion overrides are independent and leave their configuration unchanged", () => {
            var first=new ItemThrowOptions{overrideMotion=true};first.motion.arriveTime=0.3f;first.motion.arcHeight=0;
            first.motion.autoArcHeightByDistance=false;first.motion.faceMoveDirection=true;
            var second=new ItemThrowOptions{overrideMotion=true};second.motion.arriveTime=1.5f;second.motion.arcHeight=3;
            var a=new ItemThrowMover();var b=new ItemThrowMover();first.ApplyTo(a);second.ApplyTo(b);
            Eq(a.arriveTime,0.3f);Eq(b.arriveTime,1.5f);Eq(a.arcHeight,0);Eq(b.arcHeight,3);True(a.faceMoveDirection&&!b.faceMoveDirection);
            a.arrivalTiming.randomOffset=99;Eq(first.motion.arrivalTiming.randomOffset,0.15f);Eq(second.motion.arrivalTiming.randomOffset,0.15f);
        });
        Check("disabled module motion preserves prefab values and custom arrival timing still resolves weight", () => {
            var mover=new ItemThrowMover{arriveTime=2,arcHeight=4};new ItemThrowOptions().ApplyTo(mover);Eq(mover.arriveTime,2);Eq(mover.arcHeight,4);
            var options=new ItemThrowOptions{overrideMotion=true};options.motion.arriveTime=0.5f;
            options.motion.arrivalTiming.mode=ItemThrowArrivalMode.WeightAndRandomOffset;options.motion.arrivalTiming.randomOffset=0;
            options.motion.arrivalTiming.minArriveTime=0.01f;options.motion.arrivalTiming.maxArriveTime=3f;
            options.ApplyTo(mover);Eq(mover.arrivalTiming.Resolve(mover.arriveTime,mover.maxMoveTime,3,0.5f),1.1f);
        });
        Check("ten-percent module succeeds below its threshold and rejects the boundary", () => {
            var effect = new CompletionProbe(); var context = ReactionSource(effect); var module = RepeatModule(0.1f);
            True(module.TryReact(context, randomValue:()=>0.099f)); True(!module.TryReact(context, randomValue:()=>0.1f));
            True(effect.calls.Count==1); True(!SpecialStatusItemUseModule.PassesChance(0f,0f));
            True(SpecialStatusItemUseModule.PassesChance(1f,1f)); True(!SpecialStatusItemUseModule.PassesChance(float.NaN,0f));
        });
        Check("series filtering rejects a non-mineral without drawing randomness", () => {
            var effect = new CompletionProbe(); var context = ReactionSource(effect); context.sourceItemData.series=ItemSeries.Food;
            True(!RepeatModule(0.1f).TryReact(context, randomValue:()=>throw new Exception("unexpected random draw")));
            True(effect.calls.Count==0);
        });
        Check("allowed-item and exclusion settings combine with the series filter", () => {
            var context=ReactionSource(new CompletionProbe());var module=RepeatModule();
            module.allowedItems=new[]{new ItemData()};True(!module.CanReact(context));module.allowedItems=new[]{context.sourceItemData};
            True(module.CanReact(context));module.excludedItems=new[]{context.sourceItemData};True(!module.CanReact(context));
            module.excludedItems=null;module.excludedItemIds=new[]{"MineralTest"};True(!module.CanReact(context));
        });
        Check("same-item reuse preserves source bag position direction owner and execution scale", () => {
            var effect=new CompletionProbe();var manager=new BuffManager();var context=ReactionSource(effect,manager);context.damageMultiplier=2f;
            True(RepeatModule().TryReact(context));var actual=effect.calls[0];
            True(ReferenceEquals(actual.sourceItemData,context.sourceItemData)&&ReferenceEquals(actual.sourceBag,context.sourceBag));
            True(ReferenceEquals(actual.owner,context.owner)&&ReferenceEquals(actual.buffManager,manager));
            Eq(actual.usePosition.x,2f);Eq(actual.targetPosition.y,6f);Eq(actual.direction.x,1f);Eq(actual.damageMultiplier,2f);
            True(!actual.allowItemUseReactions&&!actual.consumeUseBuffs&&context.allowItemUseReactions);
            True(manager.begunUses==0&&manager.endedUses==0);
        });
        Check("configured-item reaction uses the configured item rather than the original", () => {
            var original=new CompletionProbe();var bonus=new CompletionProbe();var context=ReactionSource(original);
            var item=CompletionItem(bonus);var module=RepeatModule();module.itemSource=StatusReactionItemSource.ConfiguredItem;module.configuredItem=item;
            True(module.TryReact(context));True(original.calls.Count==0&&bonus.calls.Count==1&&ReferenceEquals(bonus.calls[0].sourceItemData,item));
        });
        Check("extra item use can consume buffs explicitly but respects parent consumption suppression", () => {
            var manager=new BuffManager();var context=ReactionSource(new CompletionProbe(),manager);var module=RepeatModule();module.consumeUseBuffs=true;
            True(module.TryReact(context));True(manager.begunUses==1&&manager.endedUses==1);
            context.consumeUseBuffs=false;True(module.TryReact(context));True(manager.begunUses==1&&manager.endedUses==1);
        });
        Check("dispatcher ignores absent and expired statuses", () => {
            var effect=new CompletionProbe();var context=ReactionSource(effect);var module=RepeatModule();var storage=new BuffStorage();
            var key=new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{module}};
            True(SpecialStatusItemUseDispatcher.Execute(context,Array.Empty<ActiveBuff>())==0);
            var status=ReactionStatus(storage,key);status.useLimitType=BuffUseLimitType.Time;status.remainTime=0f;
            True(SpecialStatusItemUseDispatcher.Execute(context,new[]{status})==0&&effect.calls.Count==0);
        });
        Check("status stacks duplicate status entries and duplicate modules produce one roll", () => {
            var effect=new CompletionProbe();var context=ReactionSource(effect);var module=RepeatModule(0.1f);var storage=new BuffStorage();int rolls=0;
            var key=new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{null,module,module}};var status=ReactionStatus(storage,key);status.stack=10;
            True(SpecialStatusItemUseDispatcher.Execute(context,new[]{status,status},randomValue:()=>{rolls++;return 0f;})==1);
            True(rolls==1&&effect.calls.Count==1);
        });
        Check("different active status keys can each react to the same original use", () => {
            var effect=new CompletionProbe();var context=ReactionSource(effect);var module=RepeatModule();var storage=new BuffStorage();
            var first=ReactionStatus(storage,new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{module}});
            var second=ReactionStatus(storage,new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{module}});
            True(SpecialStatusItemUseDispatcher.Execute(context,new[]{first,second})==2&&effect.calls.Count==2&&context.allowItemUseReactions);
        });
        Check("cleansing a later status during a reaction suppresses its captured modules", () => {
            var storage=new BuffStorage();var marker=new CompletionProbe();var context=ReactionSource(new CompletionProbe());
            var key=new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{ExtraEffects(marker)}};
            var later=ReactionStatus(storage,key);var cleanse=new CompletionProbe{onExecute=c=>storage.RemoveBuff(later)};
            var first=ReactionStatus(storage,new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{ExtraEffects(cleanse)}});
            True(SpecialStatusItemUseDispatcher.Execute(context,new[]{first,later})==1&&marker.calls.Count==0);
        });
        Check("extending the same active state during a reaction retains its remaining modules", () => {
            var storage=new BuffStorage();var context=ReactionSource(new CompletionProbe());var marker=new CompletionProbe();
            var key=new SpecialStatusDefinition();var status=ReactionStatus(storage,key);
            var extend=new CompletionProbe{onExecute=c=>storage.RegisterSpecialStatus(key,new BuffInfo{useLimitType=BuffUseLimitType.Infinite},
                null,null,new ItemEffectData(),status.target,true)};
            key.onItemUsedModules=new SpecialStatusItemUseModule[]{ExtraEffects(extend),ExtraEffects(marker)};
            True(SpecialStatusItemUseDispatcher.Execute(context,new[]{status})==2&&marker.calls.Count==1);
        });
        Check("effect-list module executes existing effects with original identity and suppresses child reactions", () => {
            var first=new CompletionProbe();var second=new CompletionProbe();var context=ReactionSource(new CompletionProbe());
            True(ExtraEffects(first,null,second).TryReact(context));True(first.calls.Count==1&&second.calls.Count==1);
            True(ReferenceEquals(first.calls[0].sourceItemData,context.sourceItemData)&&!first.calls[0].allowItemUseReactions);
        });
        Check("parent completion waits for a retained reaction item effect", () => {
            int done=0;var context=ReactionSource(new CompletionProbe());context.lifetime=new ItemEffectLifetime(onCompleted:()=>done++);
            var delayed=new CompletionProbe{wait=true};var module=RepeatModule();module.itemSource=StatusReactionItemSource.ConfiguredItem;module.configuredItem=CompletionItem(delayed);
            True(module.TryReact(context));context.lifetime.Close();True(done==0);delayed.Finish();True(done==1);
        });
        Check("bonus completion items cannot start a new state reaction even when explicitly enabled", () => {
            var follow=new CompletionProbe();var bonus=new CompletionProbe();var source=ReactionSource(new CompletionProbe());
            var module=RepeatModule();module.itemSource=StatusReactionItemSource.ConfiguredItem;module.configuredItem=CompletionItem(bonus,CompletionItem(follow));
            module.configuredItem.afterCompletionTriggerSpecialItems=true;var storage=new BuffStorage();
            var status=ReactionStatus(storage,new SpecialStatusDefinition{onItemUsedModules=new SpecialStatusItemUseModule[]{module}});
            var previous=SpecialItemManager.Instance;
            try{SpecialItemManager.Instance=new SpecialItemManager{onCall=c=>SpecialStatusItemUseDispatcher.Execute(c,new[]{status})};
                ItemEffectExecutor.ExecuteItem(source.sourceItemData,source.usePosition,source.targetPosition,source.direction,source.owner,source.sourceBag,null);
                True(SpecialItemManager.Instance.calls==1&&bonus.calls.Count==1&&follow.calls.Count==1&&!follow.calls[0].allowItemUseReactions);
            }finally{SpecialItemManager.Instance=previous;}
        });
        Check("throw module passes identity ownership bag and suppression to its flight context", () => {
            var context=ReactionSource(new CompletionProbe());var executor=new ItemThrowExecutor{itemEffectExecutor=new ItemEffectExecutor()};
            var module=RepeatModule();module.delivery=StatusReactionItemDelivery.ThrowAtImpact;
            True(module.TryReact(context,executor));var flight=executor.flights[0];
            True(ReferenceEquals(flight.sourceItemData,context.sourceItemData)&&ReferenceEquals(flight.owner,context.owner)&&ReferenceEquals(flight.sourceBag,context.sourceBag));
            Eq(flight.usePosition.y,9f);Eq(flight.targetPosition.y,6f);True(!flight.allowItemUseReactions&&!flight.consumeUseBuffs);
            var copied=flight.Copy(flight.targetPosition,flight.direction);True(!copied.allowItemUseReactions);
            var inherited=Context();inherited.InheritExecution(copied);True(!inherited.allowItemUseReactions);
        });
        Check("missing bonus item or throw executor never produces a successful reaction", () => {
            var context=ReactionSource(new CompletionProbe());var module=RepeatModule();module.itemSource=StatusReactionItemSource.ConfiguredItem;
            True(!module.TryReact(context));module.itemSource=StatusReactionItemSource.UsedItem;module.delivery=StatusReactionItemDelivery.ThrowAtImpact;
            True(!module.TryReact(context));
        });
        Check("cancelled execution and suppressed descendant refuse reactions without rolling", () => {
            var context=ReactionSource(new CompletionProbe());var module=RepeatModule(0.1f);context.allowItemUseReactions=false;
            True(!module.TryReact(context,randomValue:()=>throw new Exception("unexpected roll")));
            context.allowItemUseReactions=true;context.lifetime=new ItemEffectLifetime();context.lifetime.Close(false);
            True(!module.TryReact(context,randomValue:()=>throw new Exception("unexpected roll")));
        });
    }
}
