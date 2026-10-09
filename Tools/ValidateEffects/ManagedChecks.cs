using System;
using System.Collections.Generic;
using UnityEngine;
public static class Runner {
 static int passed;
 static void Check(string name, Action action){action();passed++;Console.WriteLine("PASS "+name);}
 static void True(bool condition){if(!condition)throw new Exception("Assertion failed");}
 static void Eq(float value,float expected){if(Math.Abs(value-expected)>0.00001f)throw new Exception($"Expected {expected}, got {value}");}
 static ItemEffectContext Context()=>new ItemEffectContext(null,null,Vector3.zero,Vector3.right,null);
 static ActiveBuff BuffRecord(BuffEffect effect,StatusDefinition status=null)
  =>new ActiveBuff(null,new BuffInfo{statusDefinition=status,useLimitType=BuffUseLimitType.Infinite},null,null,effect,null,true,false);
 private sealed class CompletionProbe : ItemEffectData {
  public readonly List<ItemEffectContext> calls=new List<ItemEffectContext>();
  public bool wait;public Action<ItemEffectContext> onExecute;private ItemEffectLease lease;
  public override void ExecuteEffect(ItemEffectContext context){calls.Add(context);if(wait)lease=context.RetainLifetime();onExecute?.Invoke(context);}
  public void Finish(bool succeeded=true){var held=lease;lease=null;if(held!=null)held.Finish(succeeded);}
 }
 static ItemData CompletionItem(CompletionProbe effect,params ItemData[] next)=>new ItemData{effectDatas=new ItemEffectData[]{effect},afterCompletionItems=next};
 static void UseCompletionItem(ItemData item,BuffManager manager=null,ItemEffectContext parent=null,EquipmentBag bag=null)
  =>ItemEffectExecutor.ExecuteItem(item,Vector3.zero,Vector3.right,Vector3.up,null,bag,manager,parent,triggerSpecialItems:false);
 public static void Main(){
 Check("even full circle starts in the chosen direction without an overlapping last shot",()=>{
  for(int i=0;i<4;i++)Eq(AttackPlacement.ResolveSpreadAngle(360,i,4,AttackSpreadDistribution.Even,AttackSpreadStartMode.FromDirection,false,0),90*i);
 });
 Check("even centered fan includes both edges and keeps a single shot on its axis",()=>{
  for(int i=0;i<4;i++)Eq(AttackPlacement.ResolveSpreadAngle(90,i,4,AttackSpreadDistribution.Even,AttackSpreadStartMode.CenteredOnDirection,false,0),-45+30*i);
  Eq(AttackPlacement.ResolveSpreadAngle(90,0,1,AttackSpreadDistribution.Even,AttackSpreadStartMode.CenteredOnDirection,false,0),0);
 });
 Check("clockwise fan reverses sweep from the selected direction",()=>{
  for(int i=0;i<4;i++)Eq(AttackPlacement.ResolveSpreadAngle(90,i,4,AttackSpreadDistribution.Even,AttackSpreadStartMode.FromDirection,true,0),-30*i);
 });
 Check("random fan respects centered and directional intervals independently of shot index",()=>{
  Eq(AttackPlacement.ResolveSpreadAngle(90,0,4,AttackSpreadDistribution.Random,AttackSpreadStartMode.CenteredOnDirection,false,0),-45);
  Eq(AttackPlacement.ResolveSpreadAngle(90,3,4,AttackSpreadDistribution.Random,AttackSpreadStartMode.CenteredOnDirection,false,1),45);
  Eq(AttackPlacement.ResolveSpreadAngle(90,2,4,AttackSpreadDistribution.Random,AttackSpreadStartMode.FromDirection,true,.25f),-22.5f);
 });
 Check("even shotgun angles coexist with independent random radius offsets",()=>{
  float[] samples={0,.25f,.75f,1};
  for(int i=0;i<4;i++){
   Eq(AttackPlacement.ResolveSpreadAngle(60,i,4,AttackSpreadDistribution.Even,AttackSpreadStartMode.CenteredOnDirection,false,samples[i]),-30+20*i);
   Eq(AttackPlacement.ResolveShotgunDistance(5,1,samples[i]),4+2*samples[i]);
  }
 });
 Check("direct modifier debuff is harmful without a status key",()=>{
  var buff=BuffRecord(new BuffEffect{harmful=true});True(buff.harmful&&buff.dispellable);True(new BuffCleanseFilter().Matches(buff));
  True(!new BuffCleanseFilter().Matches(BuffRecord(new BuffEffect())));
 });
 Check("legacy status harmful flag remains supported and either layer can forbid cleanse",()=>{
  var status=new StatusDefinition{harmful=true};var effect=new BuffEffect();var filter=new BuffCleanseFilter();
  True(filter.Matches(BuffRecord(effect,status)));
  effect.dispellable=false;True(!filter.Matches(BuffRecord(effect,status)));
  effect.dispellable=true;status.dispellable=false;True(!filter.Matches(BuffRecord(effect,status)));
 });
 Check("flag cleanse can select a beneficial buff and compares asset identity",()=>{
  var poison=new BuffFlagDefinition{displayName="Poison"};var sameName=new BuffFlagDefinition{displayName="Poison"};
  var filter=new BuffCleanseFilter{mode=BuffCleanseMode.Flags,flags=new[]{poison}};
  True(filter.Matches(BuffRecord(new BuffEffect{flags=new[]{poison}})));
  True(!filter.Matches(BuffRecord(new BuffEffect{flags=new[]{sameName}})));
 });
 Check("flag any and all support multiple independent tags",()=>{
  var poison=new BuffFlagDefinition();var curse=new BuffFlagDefinition();var buff=BuffRecord(new BuffEffect{flags=new[]{poison}});
  var filter=new BuffCleanseFilter{mode=BuffCleanseMode.Flags,flags=new[]{poison,curse}};
  True(filter.Matches(buff));filter.flagMatch=BuffFlagMatchMode.All;True(!filter.Matches(buff));
  True(filter.Matches(BuffRecord(new BuffEffect{flags=new[]{poison,curse}})));
 });
 Check("null empty and duplicate cleanse flags never broaden removal unexpectedly",()=>{
  var flag=new BuffFlagDefinition();var buff=BuffRecord(new BuffEffect{flags=new[]{flag}});
  foreach(var match in new[]{BuffFlagMatchMode.Any,BuffFlagMatchMode.All}){
   var filter=new BuffCleanseFilter{mode=BuffCleanseMode.Flags,flagMatch=match};True(!filter.Matches(buff));
   filter.flags=new BuffFlagDefinition[0];True(!filter.Matches(buff));filter.flags=new BuffFlagDefinition[]{null};True(!filter.Matches(buff));
   filter.flags=new[]{null,flag,flag};True(filter.Matches(buff));
  }
 });
 Check("harmful-or-flags and harmful-and-flags have distinct selection behavior",()=>{
  var flag=new BuffFlagDefinition();var filter=new BuffCleanseFilter{mode=BuffCleanseMode.HarmfulOrFlags,flags=new[]{flag}};
  var harmful=BuffRecord(new BuffEffect{harmful=true});var tagged=BuffRecord(new BuffEffect{flags=new[]{flag}});
  True(filter.Matches(harmful));True(filter.Matches(tagged));
  filter.mode=BuffCleanseMode.HarmfulAndFlags;True(!filter.Matches(harmful));True(!filter.Matches(tagged));
  True(filter.Matches(BuffRecord(new BuffEffect{harmful=true,flags=new[]{flag}})));
 });
 Check("non-dispellable buff resists every cleanse mode including matching tags",()=>{
  var flag=new BuffFlagDefinition();var buff=BuffRecord(new BuffEffect{harmful=true,dispellable=false,flags=new[]{flag}});
  foreach(BuffCleanseMode mode in Enum.GetValues(typeof(BuffCleanseMode)))True(!new BuffCleanseFilter{mode=mode,flags=new[]{flag}}.Matches(buff));
 });
 Check("registered classification survives asset edits until the buff is refreshed",()=>{
  var first=new BuffFlagDefinition();var second=new BuffFlagDefinition();var effect=new BuffEffect{harmful=true,flags=new[]{first}};var buff=BuffRecord(effect);
  effect.harmful=false;effect.dispellable=false;effect.flags[0]=second;
  True(buff.harmful&&buff.dispellable&&buff.HasFlag(first)&&!buff.HasFlag(second));
  buff.RegisterAgain(new BuffInfo{useLimitType=BuffUseLimitType.Infinite});
  True(!buff.harmful&&!buff.dispellable&&!buff.HasFlag(first)&&buff.HasFlag(second));
 });
 Check("refresh retains a single buff and cleansed removal reports cancellation",()=>{
  var storage=new BuffStorage();var flag=new BuffFlagDefinition();var effect=new BuffEffect{harmful=true,flags=new[]{flag}};
  var target=BuffTargetHandle.Item(new ItemData());var info=new BuffInfo{useLimitType=BuffUseLimitType.Infinite};
  var buff=storage.RegisterBuff(null,info,null,null,effect,target,true,false);
  True(ReferenceEquals(buff,storage.RegisterBuff(null,info,null,null,effect,target,true,false)));True(storage.activeBuffs.Count==1);
  BuffRemovalReason reason=BuffRemovalReason.Cancelled;buff.Removed+=(removed,why)=>reason=why;
  int completions=0;var context=Context();context.lifetime=new ItemEffectLifetime(onCompleted:()=>completions++);
  buff.completion.Track(context,null);context.lifetime.Close();True(!context.lifetime.IsFinished);
  storage.RemoveBuff(buff,BuffRemovalReason.Cleansed);True(reason==BuffRemovalReason.Cleansed&&storage.activeBuffs.Count==0&&storage.infiniteBuffs.Count==0);
  True(context.lifetime.IsFinished&&context.lifetime.IsCancelled&&completions==0);
 });
 Check("shotgun radius offset samples independent distances inside the radius band",()=>{
  Eq(AttackPlacement.ResolveShotgunDistance(5,1,0),4);Eq(AttackPlacement.ResolveShotgunDistance(5,1,.25f),4.5f);
  Eq(AttackPlacement.ResolveShotgunDistance(5,1,.75f),5.5f);Eq(AttackPlacement.ResolveShotgunDistance(5,1,1),6);
 });
 Check("shotgun zero offset preserves fixed radius",()=>{
  for(int i=0;i<=100;i++)Eq(AttackPlacement.ResolveShotgunDistance(5,0,i/100f),5);
 });
 Check("shotgun offset larger than radius cannot produce backward distances",()=>{
  Eq(AttackPlacement.ResolveShotgunDistance(.2f,1,0),0);Eq(AttackPlacement.ResolveShotgunDistance(.2f,1,.5f),.6f);
  Eq(AttackPlacement.ResolveShotgunDistance(.2f,1,1),1.2f);
 });
 Check("shotgun offset follows range scaling without changing the source stat",()=>{
  var source=new RepeatItemStat{itemRepeatRadius=5,itemRepeatShotgunRadiusOffset=1};var context=Context();context.rangeMultiplier=2;
  var scaled=EffectExecutionScaling.Apply(source,context);Eq(scaled.itemRepeatRadius,10);Eq(scaled.itemRepeatShotgunRadiusOffset,2);Eq(source.itemRepeatShotgunRadiusOffset,1);
  Eq(AttackPlacement.ResolveShotgunDistance(scaled.itemRepeatRadius,scaled.itemRepeatShotgunRadiusOffset,.25f),9);
 });
 Check("shotgun invalid radius offset and sample remain finite",()=>{
  Eq(AttackPlacement.ResolveShotgunDistance(float.NaN,float.PositiveInfinity,float.NaN),0);
  var stat=new RepeatItemStat{itemRepeatShotgunRadiusOffset=-1};stat.Clamp();Eq(stat.itemRepeatShotgunRadiusOffset,0);
  stat.itemRepeatShotgunRadiusOffset=float.NaN;stat.Clamp();Eq(stat.itemRepeatShotgunRadiusOffset,0);
 });
 Check("throw random-range endpoints and interior remain inside arrival bounds",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.RandomRange,minArriveTime=.5f,maxArriveTime=2,distribution=ItemThrowArrivalDistribution.Uniform};
  Eq(t.Resolve(1,3,0,0),.5f);Eq(t.Resolve(1,3,0,1),2);Eq(t.Resolve(1,3,0,.25f),.875f);
 });
 Check("throw short and long distributions favor opposite sides",()=>{
  var t=new ItemThrowArrivalTiming{distribution=ItemThrowArrivalDistribution.PreferShort};Eq(t.TransformSample(.25f),.0625f);
  t.distribution=ItemThrowArrivalDistribution.PreferLong;Eq(t.TransformSample(.25f),.4375f);
 });
 Check("throw middle and edge distributions have different probability concentrations",()=>{
  var t=new ItemThrowArrivalTiming();int middle=0,edges=0;
  for(int i=0;i<10000;i++){float u=(i+.5f)/10000;t.distribution=ItemThrowArrivalDistribution.PreferMiddle;float a=t.TransformSample(u);if(a>=.25f&&a<=.75f)middle++;
   t.distribution=ItemThrowArrivalDistribution.PreferEdges;float b=t.TransformSample(u);if(b>=.25f&&b<=.75f)edges++;}
  True(middle>7000);True(edges<4000);
 });
 Check("throw distribution outputs stay monotone finite and inside zero to one",()=>{
  var t=new ItemThrowArrivalTiming();foreach(ItemThrowArrivalDistribution d in System.Enum.GetValues(typeof(ItemThrowArrivalDistribution))){t.distribution=d;float previous=-1;
   for(int i=0;i<=1000;i++){float p=t.TransformSample(i/1000f);True(!float.IsNaN(p)&&p>=0&&p<=1&&p>=previous);previous=p;}}
 });
 Check("throw heavier items move the offset window later",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.WeightAndRandomOffset,minArriveTime=.5f,maxArriveTime=3,randomOffset=.2f,secondsPerWeight=.25f,distribution=ItemThrowArrivalDistribution.Uniform};
  Eq(t.Resolve(1,3,2,0),1.3f);Eq(t.Resolve(1,3,2,1),1.7f);
  for(int i=0;i<=100;i++)True(t.Resolve(1,3,3,i/100f)>=t.Resolve(1,3,1,i/100f));
 });
 Check("throw offset uses the selected distribution function",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.WeightAndRandomOffset,minArriveTime=1,maxArriveTime=2,randomOffset=.2f,secondsPerWeight=.5f,distribution=ItemThrowArrivalDistribution.PreferShort};
  Eq(t.Resolve(1,3,1,.5f),1.4f);
 });
 Check("throw clipped offset is sampled before applying the arrival bound",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.WeightAndRandomOffset,minArriveTime=1,maxArriveTime=2,randomOffset=.2f,secondsPerWeight=.5f,distribution=ItemThrowArrivalDistribution.PreferShort};
  Eq(t.Resolve(1,3,100,.5f),1.85f);Eq(t.Resolve(1,3,100,1),2);
 });
 Check("throw fixed and zero-offset modes are deterministic",()=>{
  var t=new ItemThrowArrivalTiming();Eq(t.Resolve(.7f,3,100,0),.7f);Eq(t.Resolve(.7f,3,0,1),.7f);
  t.mode=ItemThrowArrivalMode.WeightAndRandomOffset;t.randomOffset=0;Eq(t.Resolve(1,3,2,0),1.4f);Eq(t.Resolve(1,3,2,1),1.4f);
 });
 Check("throw range mode ignores item weight and offset",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.RandomRange};float a=t.Resolve(1,3,0,.3f);t.randomOffset=100;Eq(t.Resolve(1,3,100,.3f),a);
 });
 Check("throw hard movement cap and reversed bounds remain positive",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.RandomRange,minArriveTime=2,maxArriveTime=1};Eq(t.Resolve(1,.5f,0,.5f),.5f);
  Eq(ItemThrowArrivalTiming.ClampDuration(float.NaN,-1),.01f);
 });
 Check("throw invalid weight coefficient offset and sample stay finite",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.WeightAndRandomOffset,minArriveTime=2,maxArriveTime=1,secondsPerWeight=float.PositiveInfinity,randomOffset=float.NaN};
  Eq(t.Resolve(float.NaN,3,float.NaN,float.NaN),1);
 });
 Check("throw custom curve remaps samples and clamps out-of-range results",()=>{
  var t=new ItemThrowArrivalTiming{mode=ItemThrowArrivalMode.RandomRange,minArriveTime=1,maxArriveTime=2,distribution=ItemThrowArrivalDistribution.CustomCurve,customCurve=AnimationCurve.Linear(0,.2f,1,.6f)};
  Eq(t.Resolve(1,3,0,0),1.2f);Eq(t.Resolve(1,3,0,.5f),1.4f);Eq(t.Resolve(1,3,0,1),1.6f);
  t.customCurve=AnimationCurve.Linear(0,-1,1,2);Eq(t.Resolve(1,3,0,0),1);Eq(t.Resolve(1,3,0,1),2);
 });
 Check("throw missing or empty custom curve falls back to uniform",()=>{
  var t=new ItemThrowArrivalTiming{distribution=ItemThrowArrivalDistribution.CustomCurve,customCurve=null};Eq(t.TransformSample(.25f),.25f);
  t.customCurve=new AnimationCurve();Eq(t.TransformSample(.25f),.25f);
 });
 Check("cooldown seconds clamps at zero",()=>Eq(BagItemCooldownController.ChangeRemaining(3,CooldownOperation.ReduceSeconds,9),0));
 Check("cooldown fraction scales remaining",()=>Eq(BagItemCooldownController.ChangeRemaining(12,CooldownOperation.ReduceFraction,0.25f),9));
 Check("cooldown invalid values remain finite",()=>{Eq(BagItemCooldownController.ChangeRemaining(float.NaN,CooldownOperation.ReduceSeconds,1),0);Eq(BagItemCooldownController.ChangeRemaining(5,CooldownOperation.ReduceSeconds,float.NaN),5);});
 Check("ready preserves completed preparation",()=>{var c=new BagItemCooldownController();c.Init(1);c.MakeSlotReady(0);c.StartPreparationCooldownIfNeeded(0,new ItemData{cooldown=10});Eq(c.GetSlotCooldownRemain(0),0);});
 Check("reset restores initial preparation",()=>{var c=new BagItemCooldownController();c.Init(1);c.MakeSlotReady(0);c.ResetAllCooldowns(1);c.StartPreparationCooldownIfNeeded(0,new ItemData{cooldown=10});Eq(c.GetSlotCooldownRemain(0),10);});
 Check("fixed cooldown modifier subtracts seconds without changing item data",()=>{
  var item=new ItemData{cooldown=5};var manager=new BuffManager{CooldownModifiers=new[]{new FloatFieldBuffModifier{targetStatTypeName="ItemCooldownStat",fieldName="cooldown",addValue=-.2f}}};
  Eq(ItemCooldownStat.Resolve(item,null,manager).cooldown,4.8f);Eq(item.cooldown,5);
 });
 Check("cooldown modifiers add before multiplying",()=>{
  var stat=new ItemCooldownStat{cooldown=5};new FloatFieldBuffModifier{fieldName="cooldown",addValue=-.2f,multiplyValue=.5f}.ApplyTo(stat,1,null);Eq(stat.cooldown,7.2f);
 });
 Check("stacked fixed cooldown uses stack count and clamps at zero",()=>{
  var stat=new ItemCooldownStat{cooldown=5};var modifier=new FloatFieldBuffModifier{fieldName="cooldown",addValue=-.2f};modifier.ApplyTo(stat,3,null);Eq(stat.cooldown,4.4f);
  modifier.ApplyTo(stat,100,null);Eq(stat.cooldown,0);
 });
 Check("cooldown stat clones and invalid values are isolated",()=>{
  var stat=new ItemCooldownStat{cooldown=5};var clone=stat.Clone();clone.cooldown=float.NaN;clone.cooldownRecoveryRate=float.PositiveInfinity;clone.Clamp();Eq(clone.cooldown,0);Eq(clone.cooldownRecoveryRate,1);Eq(stat.cooldown,5);
  var bag=new BagCooldownStat{cooldown=-1,cooldownRecoveryRate=-1};bag.Clamp();Eq(bag.cooldown,0);Eq(bag.cooldownRecoveryRate,0);
 });
 Check("item recovery accelerates slots without accelerating bag",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.SetBagCooldown(3);c.StartBagCooldown();c.StartPreparationCooldownIfNeeded(0,5f);c.TickCooldown(1,1.2f);Eq(c.GetSlotCooldownRemain(0),3.8f);Eq(c.GetBagCooldownRemain(),2);
 });
 Check("bag recovery accelerates bag without accelerating slots",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.StartBagCooldown(3);c.StartPreparationCooldownIfNeeded(0,5f);c.TickCooldown(1,1,2);Eq(c.GetSlotCooldownRemain(0),4);Eq(c.GetBagCooldownRemain(),1);
 });
 Check("slot recovery callback preserves per-item scope",()=>{
  var c=new BagItemCooldownController();c.Init(2);c.StartPreparationCooldownIfNeeded(0,5f);c.StartPreparationCooldownIfNeeded(1,5f);c.TickCooldown(1,index=>index==0?2:1);Eq(c.GetSlotCooldownRemain(0),3);Eq(c.GetSlotCooldownRemain(1),4);
 });
 Check("zero item recovery does not freeze bag",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.StartBagCooldown(3);c.StartPreparationCooldownIfNeeded(0,5f);c.TickCooldown(1,0);Eq(c.GetSlotCooldownRemain(0),5);Eq(c.GetBagCooldownRemain(),2);
 });
 Check("zero bag recovery does not freeze items",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.StartBagCooldown(3);c.StartPreparationCooldownIfNeeded(0,5f);c.TickCooldown(1,1,0);Eq(c.GetSlotCooldownRemain(0),4);Eq(c.GetBagCooldownRemain(),3);
 });
 Check("slot progress uses captured buffed duration and survives slot resize",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.StartPreparationCooldownIfNeeded(0,4.8f);Eq(c.GetSlotCooldownRatio(0),1);c.TickCooldown(1);c.SyncSlotCount(2);Eq(c.GetSlotCooldownRatio(0),3.8f/4.8f);
 });
 Check("bag progress uses captured duration after base value changes",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.StartBagCooldown(2.8f);Eq(c.GetBagCooldownRatio(),1);c.TickCooldown(1);c.SetBagCooldown(3);Eq(c.GetBagCooldownRatio(),1.8f/2.8f);
 });
 Check("fixed reduction expiry changes next preparation and preserves current clock",()=>{
  var item=new ItemData{cooldown=5};var manager=new BuffManager{CooldownModifiers=new[]{new FloatFieldBuffModifier{fieldName="cooldown",addValue=-.2f}}};var c=new BagItemCooldownController();c.Init(1);
  c.StartPreparationCooldownIfNeeded(0,ItemCooldownStat.Resolve(item,null,manager).cooldown);c.TickCooldown(1);manager.CooldownModifiers=null;
  c.StartPreparationCooldownIfNeeded(0,ItemCooldownStat.Resolve(item,null,manager).cooldown);Eq(c.GetSlotCooldownRemain(0),3.8f);
  c.ResetSlotPreparation(1);c.StartPreparationCooldownIfNeeded(0,ItemCooldownStat.Resolve(item,null,manager).cooldown);Eq(c.GetSlotCooldownRemain(0),5);
 });
 Check("item and bag stat types filter modifiers independently without consuming uses",()=>{
  var manager=new BuffManager{LastConsume=true,CooldownModifiers=new[]{new FloatFieldBuffModifier{targetStatTypeName="BagCooldownStat",fieldName="cooldown",addValue=-.2f}}};
  Eq(ItemCooldownStat.Resolve(new ItemData{cooldown=5},null,manager).cooldown,5);True(!manager.LastConsume);
  manager.LastConsume=true;Eq(BagCooldownStat.Resolve(3,new EquipmentBag(),manager).cooldown,2.8f);True(!manager.LastConsume);
 });
 Check("item recovery field adds to its base of one",()=>{
  var manager=new BuffManager{CooldownModifiers=new[]{new FloatFieldBuffModifier{fieldName="cooldownRecoveryRate",addValue=.2f}}};Eq(ItemCooldownStat.Resolve(new ItemData{cooldown=5},null,manager).cooldownRecoveryRate,1.2f);
 });
 Check("invalid recovery and delta times keep both clocks finite",()=>{
  var c=new BagItemCooldownController();c.Init(1);c.StartBagCooldown(3);c.StartPreparationCooldownIfNeeded(0,5f);c.TickCooldown(float.NaN);Eq(c.GetSlotCooldownRemain(0),5);c.TickCooldown(1,float.NaN,float.NaN);Eq(c.GetSlotCooldownRemain(0),4);Eq(c.GetBagCooldownRemain(),2);
 });
 Check("all-items target matches items in different bags but excludes bag-only queries",()=>{
  var target=BuffTargetHandle.AllItems();var a=new EquipmentBag();var b=new EquipmentBag();
  True(target.Matches(BuffQueryContext.ForItem(new ItemData(),a)));True(target.Matches(BuffQueryContext.ForItem(new ItemData(),b)));True(!target.Matches(BuffQueryContext.ForBag(a)));
 });
 Check("all-bags target matches different bags but excludes item queries",()=>{
  var target=BuffTargetHandle.AllBags();var a=new EquipmentBag();var b=new EquipmentBag();
  True(target.Matches(BuffQueryContext.ForBag(a)));True(target.Matches(BuffQueryContext.ForBag(b)));True(!target.Matches(BuffQueryContext.ForItem(new ItemData(),a)));True(!target.Matches(BuffQueryContext.ForBag(null)));
 });
 Check("source-bag target retains its scope for both bag and item stats",()=>{
  var a=new EquipmentBag();var b=new EquipmentBag();var target=BuffTargetHandle.Bag(a);
  True(target.Matches(BuffQueryContext.ForBag(a)));True(!target.Matches(BuffQueryContext.ForBag(b)));
  True(target.Matches(BuffQueryContext.ForItem(new ItemData(),a)));True(!target.Matches(BuffQueryContext.ForItem(new ItemData(),b)));
 });
 Check("completion items wait for all retained effects without an end visual",()=>{
  var first=new CompletionProbe{wait=true};var second=new CompletionProbe{wait=true};var next=new CompletionProbe();
  var source=new ItemData{effectDatas=new ItemEffectData[]{first,second},afterCompletionItems=new[]{CompletionItem(next)}};
  UseCompletionItem(source);True(next.calls.Count==0);first.Finish();True(next.calls.Count==0);second.Finish();True(next.calls.Count==1);second.Finish();True(next.calls.Count==1);
 });
 Check("forced child cancellation suppresses completion items",()=>{
  var first=new CompletionProbe{wait=true};var second=new CompletionProbe{wait=true};var next=new CompletionProbe();
  var source=new ItemData{effectDatas=new ItemEffectData[]{first,second},afterCompletionItems=new[]{CompletionItem(next)}};
  UseCompletionItem(source);first.Finish(false);second.Finish();True(next.calls.Count==0);
 });
 Check("battle generation reset suppresses completion items",()=>{
  var hold=new CompletionProbe{wait=true};var next=new CompletionProbe();UseCompletionItem(CompletionItem(hold,CompletionItem(next)));
  ItemEffectRuntime.CancelAll();hold.Finish();True(next.calls.Count==0);
 });
 Check("completion item list is captured before the source runs",()=>{
  var hold=new CompletionProbe{wait=true};var next=new CompletionProbe();var replacement=new CompletionProbe();var source=CompletionItem(hold,CompletionItem(next));
  UseCompletionItem(source);source.afterCompletionItems[0]=CompletionItem(replacement);hold.Finish();True(next.calls.Count==1&&replacement.calls.Count==0);
 });
 Check("completion self and indirect cycles stop before using an ancestor again",()=>{
  var recordA=new CompletionProbe();var recordB=new CompletionProbe();var a=CompletionItem(recordA);var b=CompletionItem(recordB);
  a.afterCompletionItems=new[]{a,b};b.afterCompletionItems=new[]{a};UseCompletionItem(a);True(recordA.calls.Count==1&&recordB.calls.Count==1);
 });
 Check("completion siblings can repeat while null and empty items are skipped",()=>{
  var next=new CompletionProbe();var target=CompletionItem(next);UseCompletionItem(CompletionItem(new CompletionProbe(),null,new ItemData(),target,target));True(next.calls.Count==2);
 });
 Check("outer lifetime waits for asynchronous completion item chains",()=>{
  var b=new CompletionProbe{wait=true};var c=new CompletionProbe{wait=true};int finished=0;var outer=new ItemEffectLifetime(onCompleted:()=>finished++,cancelOnChildFailure:true);
  var parent=Context();parent.lifetime=outer;UseCompletionItem(CompletionItem(new CompletionProbe(),CompletionItem(b,CompletionItem(c))),parent:parent);
  outer.Close();True(finished==0);b.Finish();True(finished==0&&c.calls.Count==1);c.Finish();True(finished==1);
 });
 Check("completion visual starts before followup item use",()=>{
  var order=new List<int>();var next=new CompletionProbe{onExecute=context=>order.Add(2)};var source=CompletionItem(new CompletionProbe(),CompletionItem(next));
  source.endVisualData=new EffectVisualData{played=()=>order.Add(1)};UseCompletionItem(source);True(order.Count==2&&order[0]==1&&order[1]==2);
 });
 Check("automatic completion item use defaults avoid consuming buffs or triggering specials",()=>{
  var manager=new BuffManager();var next=new CompletionProbe();var source=CompletionItem(new CompletionProbe(),CompletionItem(next));var previous=SpecialItemManager.Instance;
  try{SpecialItemManager.Instance=new SpecialItemManager();UseCompletionItem(source,manager);True(manager.begunUses==1&&manager.endedUses==1&&SpecialItemManager.Instance.calls==0);True(!next.calls[0].consumeUseBuffs);
   source.afterCompletionConsumeUseBuffs=true;source.afterCompletionTriggerSpecialItems=true;UseCompletionItem(source,manager);True(manager.begunUses==3&&manager.endedUses==3&&SpecialItemManager.Instance.calls==1);}
  finally{SpecialItemManager.Instance=previous;}
 });
 Check("completion item preserves bag direction and scale but uses its own item identity",()=>{
  var next=new CompletionProbe();var target=CompletionItem(next);var source=CompletionItem(new CompletionProbe(),target);var bag=new EquipmentBag();var parent=Context();parent.damageMultiplier=1.5f;
  UseCompletionItem(source,parent:parent,bag:bag);var actual=next.calls[0];True(ReferenceEquals(actual.sourceItemData,target)&&ReferenceEquals(actual.sourceBag,bag));Eq(actual.usePosition.x,1);Eq(actual.targetPosition.x,1);Eq(actual.direction.y,1);Eq(actual.damageMultiplier,1.5f);
 });
 Check("long completion chains stop at the execution depth limit",()=>{
  var probes=new List<CompletionProbe>();ItemData next=null;for(int i=0;i<100;i++){var probe=new CompletionProbe();probes.Add(probe);next=CompletionItem(probe,next);}
  UseCompletionItem(next);int calls=0;foreach(var probe in probes)calls+=probe.calls.Count;True(calls>0&&calls<100);
 });
 Check("scope waits for retained child",()=>{int done=0;var s=new ItemEffectLifetime(onCompleted:()=>done++);var l=s.Retain();s.Close();True(done==0);l.Finish();True(done==1);l.Finish();True(done==1);});
 Check("scope cancellation suppresses completion",()=>{int done=0;var s=new ItemEffectLifetime(onCompleted:()=>done++);var l=s.Retain();s.Close();l.Cancel();True(done==0&&s.IsCancelled);});
 Check("generation cancellation invalidates copies",()=>{var c=Context();var copy=c.Copy(Vector3.zero,Vector3.right);ItemEffectRuntime.CancelAll();True(!c.CanContinue&&!copy.CanContinue);});
 Check("execution scaling leaves asset and snapshot intact",()=>{var c=Context();c.damageMultiplier=.5f;c.rangeMultiplier=.25f;c.durationMultiplier=2;var s=new DamageAreaAttackStat{damageAreaPower=10,damageAreaRange=4,damageAreaLifeTime=3};var r=EffectExecutionScaling.Apply(s,c);Eq(r.damageAreaPower,5);Eq(r.damageAreaRange,1);Eq(r.damageAreaLifeTime,6);Eq(s.damageAreaPower,10);Eq(s.damageAreaRange,4);});
 Check("damage override applies before multiplier",()=>{var c=Context();c.damageOverride=8;c.damageMultiplier=2;var r=EffectExecutionScaling.Apply(new DamageAreaAttackStat{damageAreaPower=100},c);Eq(r.damageAreaPower,16);});
 Check("execution scale preserves intervals and counts",()=>{var c=Context();c.rangeMultiplier=.5f;c.durationMultiplier=.5f;var r=EffectExecutionScaling.Apply(new RepeatItemStat{itemRepeatCount=5,itemRepeatInterval=2,itemRepeatRadius=8},c);Eq(r.itemRepeatCount,5);Eq(r.itemRepeatInterval,2);Eq(r.itemRepeatRadius,4);});
 Check("context copies share hit use token and multipliers",()=>{var c=Context();c.damageMultiplier=.5f;c.healingMultiplier=2;var copy=c.Copy(Vector3.zero,Vector3.right);True(ReferenceEquals(c.hitUseState,copy.hitUseState));Eq(copy.damageMultiplier,.5f);Eq(copy.healingMultiplier,2);});
 Check("same-use execution budget bounds repeated child calls",()=>{var c=Context();var effect=new ItemEffectData();for(int i=0;i<4096;i++){True(c.TryBeginEffectExecution(effect));c.EndEffectExecution(effect);}True(!c.TryBeginEffectExecution(effect));});
 Check("execution recursion prevents ancestor effect",()=>{var c=Context();var effect=new ItemEffectData();c.currentEffectData=effect;True(!c.Copy(Vector3.zero,Vector3.right).TryBeginEffectExecution(effect));});
 Check("first-hit-per-use shared across attacks",()=>{var c=Context();var h=new HitEffectData();var a=new HitEffectAttackState();var b=new HitEffectAttackState();True(a.TryClaim(h,HitEffectApplyMode.FirstHitPerUse,new HitEffectContext(null,c)));True(!b.TryClaim(h,HitEffectApplyMode.FirstHitPerUse,new HitEffectContext(null,c.Copy(Vector3.zero,Vector3.right))));});
 Check("first-hit-per-attack independent across attacks",()=>{var c=Context();var h=new HitEffectData();var a=new HitEffectAttackState();var b=new HitEffectAttackState();True(a.TryClaim(h,HitEffectApplyMode.FirstHitPerAttack,new HitEffectContext(null,c)));True(!a.TryClaim(h,HitEffectApplyMode.FirstHitPerAttack,new HitEffectContext(null,c)));True(b.TryClaim(h,HitEffectApplyMode.FirstHitPerAttack,new HitEffectContext(null,c)));});
 Check("target life separates pooled enemy hit records",()=>{var enemy=new Enemy{HitEffectLifeId=1};var c=Context();var h=new HitEffectData();var a=new HitEffectAttackState();True(a.TryClaim(h,HitEffectApplyMode.FirstHitOnly,new HitEffectContext(enemy,c)));True(!a.TryClaim(h,HitEffectApplyMode.FirstHitOnly,new HitEffectContext(enemy,c)));enemy.HitEffectLifeId=2;True(a.TryClaim(h,HitEffectApplyMode.FirstHitOnly,new HitEffectContext(enemy,c)));});
 Check("cancellation reaches existing child scope",()=>{var parent=new ItemEffectLifetime();var child=new ItemEffectLifetime(parent);parent.Close(false);True(child.IsCancelled);child.Close();});
 Check("weighted 70/30 selects one mutually exclusive outcome",()=>{var a=new ItemData();var b=new ItemData();var effect=new WeightedRandomEffect{entries=new[]{new WeightedEffectEntry{item=a,weight=70},new WeightedEffectEntry{item=b,weight=30}}};True(ReferenceEquals(effect.SelectEntries(.6999f)[0].item,a));True(ReferenceEquals(effect.SelectEntries(.701f)[0].item,b));True(effect.SelectEntries(.2f).Count==1);});
 Check("weighted ignores nonfinite and negative weights",()=>{var a=new ItemData();var effect=new WeightedRandomEffect{entries=new[]{new WeightedEffectEntry{item=new ItemData(),weight=float.PositiveInfinity},new WeightedEffectEntry{item=new ItemData(),weight=-5},new WeightedEffectEntry{item=a,weight=1}}};True(ReferenceEquals(effect.SelectEntries(float.NaN)[0].item,a));});
 Check("weighted batch without replacement is bounded by available payloads",()=>{var a=new ItemData();var b=new ItemData();var effect=new WeightedRandomEffect{selectionCount=5,allowDuplicates=false,entries=new[]{new WeightedEffectEntry{item=a,weight=70},new WeightedEffectEntry{item=b,weight=30}}};var got=effect.SelectEntries(.2f);True(got.Count==2&&!ReferenceEquals(got[0].item,got[1].item));});
 Check("weighted empty payload never produces outcome",()=>{var effect=new WeightedRandomEffect{entries=new[]{new WeightedEffectEntry{weight=70}}};True(effect.SelectEntries(.4f).Count==0);});
 Check("time stop overlaps preserve independent reasons",()=>{using(var a=TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions))using(var b=TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions|TimeStopTargets.EnemyProjectiles)){a.Dispose();True(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions));True(TimeStopRuntime.IsStopped(TimeStopTargets.EnemyProjectiles));}True(!TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions));});
 Check("time stop cancelled context stops contributing",()=>{var context=Context();context.lifetime=new ItemEffectLifetime();using(var h=TimeStopRuntime.Acquire(TimeStopTargets.EnemySpawning,context)){True(TimeStopRuntime.IsStopped(TimeStopTargets.EnemySpawning));context.lifetime.Close(false);True(!TimeStopRuntime.IsStopped(TimeStopTargets.EnemySpawning));}});
 Check("time stop generation cancellation invalidates all prior handles",()=>{using(var h=TimeStopRuntime.Acquire(TimeStopTargets.EnemyActions)){ItemEffectRuntime.CancelAll();True(!h.IsValid&&!TimeStopRuntime.IsStopped(TimeStopTargets.EnemyActions));h.Dispose();}});
 Check("strict completion skips followup on cancelled descendant",()=>{int done=0;var root=new ItemEffectLifetime(onCompleted:()=>done++,cancelOnChildFailure:true);var child=new ItemEffectLifetime(root);var grandchild=new ItemEffectLifetime(child);root.Close();child.Close();grandchild.Close(false);True(root.IsCancelled&&done==0);});
 Check("default parent keeps unrelated child failure isolated",()=>{int done=0;var root=new ItemEffectLifetime(onCompleted:()=>done++);var child=new ItemEffectLifetime(root);root.Close();child.Close(false);True(!root.IsCancelled&&done==1);});
 Check("execution multiplier scales dynamic additions too",()=>{var c=Context();c.buffManager=new BuffManager{DynamicDamageAdd=10};c.damageMultiplier=.5f;var effect=new ItemEffectData();var actual=c.GetCurrentStat(effect,new DamageAreaAttackStat{damageAreaPower=10});Eq(actual.damageAreaPower,10);});
 Check("automatic child snapshot suppresses use-buff consumption",()=>{var c=Context();c.buffManager=new BuffManager();c.consumeUseBuffs=false;c.GetSnapshotStat(new ItemEffectData(),new DamageAreaAttackStat());True(!c.buffManager.LastConsume);True(!c.Copy(Vector3.zero,Vector3.right).consumeUseBuffs);});
 Console.WriteLine($"{passed} managed logic checks passed using actual production code and Unity reference math.");
 }
}
