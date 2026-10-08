using UnityEngine;
public class ItemData { public float cooldown; public ItemSeries series; public float Cooldown=>cooldown; public string GetDataName()=>"Managed Item"; }
public enum ItemSeries { None, Weapon, Food }
public class InventoryItem { public ItemData itemData; public int amount; }
public class EquipmentBag { public string name; public System.Collections.Generic.List<InventoryItem> equippedItems; }
public class ItemEffectData { public virtual void Prepare(ItemEffectContext c){} public virtual void ExecuteEffect(ItemEffectContext c){} public void Execute(ItemEffectContext c){} }
public class BuffManager {
 public bool LastConsume;
 public float DynamicDamageAdd;
 public FloatFieldBuffModifier[] CooldownModifiers;
 public T GetBuffedStatForItem<T>(T stat, ItemData item, EquipmentBag bag, BuffCalculationMode mode, bool count=false) where T:class,IGameStat<T> {
  LastConsume=count;T copy=stat.Clone();var damage=copy as DamageAreaAttackStat;
  if(damage!=null && mode==BuffCalculationMode.DynamicOnly)damage.damageAreaPower+=DynamicDamageAdd;
  ApplyCooldownModifiers(copy);
  return copy;
 }
 public T GetBuffedStat<T>(T stat, BuffQueryContext query, BuffCalculationMode mode, bool count=false) where T:class,IGameStat<T> {
  LastConsume=count;T copy=stat.Clone();ApplyCooldownModifiers(copy);return copy;
 }
 private void ApplyCooldownModifiers<T>(T stat) where T:class,IGameStat<T> {
  if(CooldownModifiers==null)return;
  foreach(var modifier in CooldownModifiers)if(modifier.CanApplyTo(stat,null))modifier.ApplyAdditiveTo(stat,1,null);
  foreach(var modifier in CooldownModifiers)if(modifier.CanApplyTo(stat,null))modifier.ApplyMultiplicativeTo(stat,1,null);
  stat.Clamp();
 }
}
public enum CooldownOperation { ReduceSeconds, ReduceFraction, Ready }
public class EffectVisualData { public void Play(EffectVisualContext c){} }
public class EffectVisualContext { public EffectVisualContext(Vector3 p, Quaternion q){} }
public class Enemy { public int HitEffectLifeId; public bool CanReceiveHitEffects=true; public int damages; public SimpleTransform transform=new SimpleTransform(); public void TakeDamage(float d, ItemEffectContext source=null){damages++;} }
public class SimpleTransform { public Vector3 position; }
public class HitEffectData { public int hits; public void TryExecute(HitEffectContext h){hits++;} }
public class BuffEffect : ItemEffectData { }
public class SummonItemThrower { }

public static class ItemEffectUtility { public static ItemEffectData[] Copy(ItemEffectData[] x)=>x!=null?(ItemEffectData[])x.Clone():null; public static void Execute(ItemEffectData[] e,ItemEffectContext c){} }
public static class ItemEffectExecutor { public static void ExecuteItem(ItemData item,Vector3 u,Vector3 t,Vector3 d,GameObject owner,EquipmentBag bag,BuffManager manager,ItemEffectContext parent=null,bool triggerSpecialItems=true,bool isThrownItem=false,bool consumeUseBuffs=true){} }
// ScriptableObject/RNG substitutes allow arithmetic checks without loading the Unity engine.
namespace UnityEngine {
 public class ScriptableObject { }
 public static class Random { private static System.Random rng=new System.Random(42); public static float value=>(float)rng.NextDouble(); }
}
