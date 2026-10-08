using UnityEngine;
public class ItemData { public float cooldown; public float Cooldown=>cooldown; }
public class InventoryItem { public ItemData itemData; public int amount; }
public class EquipmentBag { public System.Collections.Generic.List<InventoryItem> equippedItems; }
public class ItemEffectData { public virtual void Prepare(ItemEffectContext c){} public virtual void ExecuteEffect(ItemEffectContext c){} public void Execute(ItemEffectContext c){} }
public class BuffManager {
 public bool LastConsume;
 public float DynamicDamageAdd;
 public T GetBuffedStatForItem<T>(T stat, ItemData item, EquipmentBag bag, BuffCalculationMode mode, bool count=false) where T:class,IGameStat<T> {
  LastConsume=count;T copy=stat.Clone();var damage=copy as DamageAreaAttackStat;
  if(damage!=null && mode==BuffCalculationMode.DynamicOnly)damage.damageAreaPower+=DynamicDamageAdd;
  return copy;
 }
}
public enum BuffCalculationMode { All, SnapshotOnly, DynamicOnly }
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
namespace UnityEngine { public static class Random { private static System.Random rng=new System.Random(42); public static float value=>(float)rng.NextDouble(); } }
