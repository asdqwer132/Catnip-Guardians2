using System.Collections.Generic;
using UnityEngine;

// One flat item list. Cooldowns belong to items; there are no bag IDs or use cycles.
public sealed class ItemUseController : MonoBehaviour
{
    public List<InventoryItem> items = new List<InventoryItem>();
    public ItemThrowExecutor throwExecutor;
    public GameObject owner;
    public BuffManager buffManager;
    private readonly Dictionary<ItemData, float> cooldowns = new Dictionary<ItemData, float>();
    private readonly List<ItemData> ticking = new List<ItemData>();
    private static readonly HashSet<ItemUseController> active = new HashSet<ItemUseController>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRegistry() => active.Clear();
    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);
    private void Update() => Tick(Time.deltaTime);
    public void Init() => cooldowns.Clear();
    public float GetRemaining(ItemData item) => item != null && cooldowns.TryGetValue(item, out float value) ? value : 0f;
    public void Tick(float seconds)
    {
        if (seconds <= 0) return;
        ticking.Clear(); ticking.AddRange(cooldowns.Keys);
        foreach (var item in ticking)
        {
            if (item == null) { cooldowns.Remove(item); continue; }
            var stat = ItemCooldownStat.Resolve(item, buffManager != null ? buffManager : BuffManager.instance);
            cooldowns[item] = Mathf.Max(0, cooldowns[item] - seconds * stat.cooldownRecoveryRate);
        }
    }
    public bool TryUse(Vector3 start, Vector3 target)
    {
        if (throwExecutor == null) return false;
        foreach (var entry in items)
        {
            var item = entry?.itemData;
            if (item == null || entry.amount <= 0 || GetRemaining(item) > 0) continue;
            if (!throwExecutor.Throw(item, start, target, owner != null ? owner : gameObject)) return false;
            cooldowns[item] = ItemCooldownStat.Resolve(item, buffManager != null ? buffManager : BuffManager.instance).cooldown;
            return true;
        }
        return false;
    }
    public void StartCooldown(ItemData item, float seconds)
    { if (item != null) cooldowns[item] = Mathf.Max(0, seconds); }
    public static void ApplyCooldownChange(CooldownControlEffect effect)
    {
        foreach (var controller in active)
        {
            controller.ticking.Clear(); controller.ticking.AddRange(controller.cooldowns.Keys);
            foreach (var item in controller.ticking)
            {
                if (!effect.Matches(item)) continue;
                float remaining = controller.GetRemaining(item);
                switch (effect.operation)
                {
                    case CooldownOperation.Ready: remaining = 0; break;
                    case CooldownOperation.ReduceSeconds: remaining -= Mathf.Max(0, effect.amount); break;
                    case CooldownOperation.ReduceFraction: remaining *= 1f - Mathf.Clamp01(effect.amount); break;
                }
                controller.cooldowns[item] = Mathf.Max(0, remaining);
            }
        }
    }
}
