using UnityEngine;
public class ItemData {
 public string dataId;
 public float cooldown; public ItemSeries series; public Sprite icon; public float Cooldown=>cooldown; public string GetDataName()=>"Managed Item";
 public ItemEffectData[] effectDatas; public EffectVisualData endVisualData; public bool endVisualAtOwner;
 public ItemData[] afterCompletionItems; public bool afterCompletionItemsAtOwner;
 public bool afterCompletionConsumeUseBuffs, afterCompletionTriggerSpecialItems;
}
public enum ItemSeries { None, Weapon, Food, Mineral }
public enum PlayerStatusList { arrow, star }
public class InventoryItem { public ItemData itemData; public int amount; }
public class EquipmentBag { public string name; public System.Collections.Generic.List<InventoryItem> equippedItems; }
public class ItemEffectData : ScriptableObject { public virtual void Prepare(ItemEffectContext c){} public virtual void ExecuteEffect(ItemEffectContext c){} public void Execute(ItemEffectContext c)=>ExecuteEffect(c); public bool CanExecute(ItemEffectContext c)=>c!=null&&c.CanContinue; }
public class BuffManager {
 public static BuffManager instance;
 public int begunUses, endedUses;
 public BuffItemUseToken BeginItemUse(ItemData item, EquipmentBag bag){begunUses++;return new BuffItemUseToken();}
 public void EndItemUse(BuffItemUseToken token,bool succeeded){endedUses++;}
 public bool LastConsume;
 public float DynamicDamageAdd;
 public FloatFieldBuffModifier[] CooldownModifiers;
 public T GetBuffedStatForItem<T>(T stat, ItemData item, EquipmentBag bag, BuffCalculationMode mode, bool count=false) where T:class,IGameStat<T> {
  LastConsume=count;T copy=BuffStatCloneUtility.Clone(stat);var damage=copy as DamageAreaAttackStat;
  if(damage!=null && mode==BuffCalculationMode.DynamicOnly)damage.damageAreaPower+=DynamicDamageAdd;
  ApplyCooldownModifiers(copy);
  return copy;
 }
 public T GetBuffedStat<T>(T stat, BuffQueryContext query, BuffCalculationMode mode, bool count=false) where T:class,IGameStat<T> {
  LastConsume=count;T copy=BuffStatCloneUtility.Clone(stat);ApplyCooldownModifiers(copy);return copy;
 }
 private void ApplyCooldownModifiers<T>(T stat) where T:class,IGameStat<T> {
  if(CooldownModifiers==null)return;
  foreach(var modifier in CooldownModifiers)if(modifier.CanApplyTo(stat,null))modifier.ApplyAdditiveTo(stat,1,null);
  foreach(var modifier in CooldownModifiers)if(modifier.CanApplyTo(stat,null))modifier.ApplyMultiplicativeTo(stat,1,null);
  stat.Clamp();
 }
}
public enum CooldownOperation { ReduceSeconds, ReduceFraction, Ready }
public struct BuffItemUseToken { }
public class EffectVisualData { public System.Action played; public void Play(EffectVisualContext c)=>played?.Invoke(); }
public class EffectVisualContext { public EffectVisualContext(Vector3 p, Quaternion q){} }
public class Enemy { public int HitEffectLifeId; public bool CanReceiveHitEffects=true; public int damages; public SimpleTransform transform=new SimpleTransform(); public void TakeDamage(float d, ItemEffectContext source=null){damages++;} }
public class SimpleTransform { public Vector3 position; }
public class HitEffectData { public int hits; public void TryExecute(HitEffectContext h){hits++;} }
public class BuffEffect : ItemEffectData { public BuffClassificationDefinition buffClassification; public bool harmful; public bool dispellable=true; public BuffFlagDefinition[] flags; public Sprite buffIcon; }
public class GrantSpecialStatusEffect : ItemEffectData { public BuffClassificationDefinition buffClassification; }
public class SummonActor { }

public class SpecialItemManager { public static SpecialItemManager Instance; public int calls; public System.Action<ItemEffectContext> onCall; public void Call(ItemEffectContext context){calls++;onCall?.Invoke(context);} }
public class ItemThrowExecutor {
 public ItemEffectExecutor itemEffectExecutor;
 public ItemThrowOptions lastOptions;
 public readonly System.Collections.Generic.List<ItemEffectContext> flights=new System.Collections.Generic.List<ItemEffectContext>();
 public bool Throw(ItemData item,Vector3 start,Vector3 target,GameObject owner,EquipmentBag bag,int cycle,ItemEffectContext parentContext=null,bool triggerSpecialItems=true,ItemThrowOptions options=null) {
  lastOptions=options;
  var flight=new ItemEffectContext(owner,item,start,target,bag,buffManager:parentContext?.buffManager,direction:target-start);
  flight.InheritExecution(parentContext);flights.Add(flight);return true;
 }
}
public class ItemThrowMover {
 public float arriveTime,maxMoveTime,arcHeight,minArcHeight,maxArcHeight,arcHeightDistanceMultiplier,arcPeakProgress,spinSpeed,directionAngleOffset;
 public bool autoArcHeightByDistance,spinWhileMoving,faceMoveDirection;
 public ItemThrowArrivalTiming arrivalTiming;
}
public static class ReactiveGroundArea { public static bool NotifyItemLanded(ItemEffectContext context)=>false; }
// Scene objects/ScriptableObject/RNG substitutes avoid Unity native calls in managed checks.
namespace UnityEngine {
 // Quaternion.Euler invokes a native Unity ECall. The managed harness only substitutes
 // the planar rotation used by AttackPlacement; engine-backed tests use Unity's Quaternion.
 public struct Quaternion {
  private float radians;
  public static Quaternion identity=>new Quaternion();
  public static Quaternion Euler(float x,float y,float z) {
   if(x!=0f||y!=0f)throw new System.NotSupportedException("Managed checks support only planar Z rotation.");
   return new Quaternion{radians=z*Mathf.Deg2Rad};
  }
  public static Vector3 operator*(Quaternion rotation,Vector3 point) {
   float cosine=Mathf.Cos(rotation.radians),sine=Mathf.Sin(rotation.radians);
   return new Vector3(point.x*cosine-point.y*sine,point.x*sine+point.y*cosine,point.z);
  }
 }
 public class Object { public string name; }
 public class Component : Object {
  public GameObject gameObject;public Transform transform;
  public T GetComponent<T>() where T:class=>null;
  public T GetComponentInParent<T>() where T:class=>null;
 }
 public class MonoBehaviour : Component { }
 public class Transform : Component { public Vector3 position; }
 public class GameObject : Object {
  public Transform transform=new Transform();
  public T GetComponent<T>() where T:class=>null;
  public T GetComponentInParent<T>() where T:class=>null;
 }
 public class ScriptableObject : Object { }
 public class AnimationCurve {
  private System.Func<float,float> evaluate;public int length { get; private set; }
  public AnimationCurve() { }
  public static AnimationCurve Linear(float startTime,float startValue,float endTime,float endValue)
   =>new AnimationCurve{length=2,evaluate=u=>Mathf.Lerp(startValue,endValue,Mathf.InverseLerp(startTime,endTime,u))};
  public float Evaluate(float u)=>evaluate!=null?evaluate(u):0;
 }
 public static class Random {
  private static System.Random rng=new System.Random(42); public static int DrawCount;
  public static float value { get { DrawCount++; return (float)rng.NextDouble(); } }
  public static float Range(float minimum,float maximum)=>Mathf.Lerp(minimum,maximum,value);
 }
}
