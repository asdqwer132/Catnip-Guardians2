using System.Collections.Generic;
using UnityEngine;

public sealed class ReactiveGroundArea : AttackObject<ReactiveGroundStat>
{
    private static readonly List<ReactiveGroundArea> active = new List<ReactiveGroundArea>();
    private ItemEffectContext context;
    private ReactiveGroundStat stat;
    private ItemEffectData[] defaults, specials;
    private ItemData[] accepted;
    private Transform visual;
    private Vector3 visualScale;
    private bool sameOwner, replaceIncoming, scaleVisual, initialized, reacting, finishing;
    private float elapsed, tickElapsed, nextReaction, specialUntil;

    public bool IsSpecialActive => initialized && Time.time < specialUntil;

    public void Init(ReactiveGroundEffect effect, ItemEffectContext execution)
    {
        context = execution.Copy(transform.position, execution.direction);
        defaults = ItemEffectUtility.Copy(effect.defaultEffects);
        specials = ItemEffectUtility.Copy(effect.specialEffects);
        accepted = effect.acceptedItems != null ? (ItemData[])effect.acceptedItems.Clone() : null;
        sameOwner = effect.onlySameOwner;
        replaceIncoming = effect.replaceIncomingItemEffects;
        scaleVisual = effect.scaleVisualByRadius;
        if (effect.areaVisualPrefab != null)
        {
            visual = Instantiate(effect.areaVisualPrefab, transform).transform;
            visual.localPosition = Vector3.zero;
            visualScale = visual.localScale;
        }
        BindLifetime(context);
        InitWithSnapshotAndDynamicBuff(context.GetSnapshotStat(effect, effect.groundStat),
            context.sourceItemData, context.sourceBag, context.buffManager, context.owner);
        initialized = true;
        active.Add(this);
        ItemEffectUtility.Execute(defaults, context.Copy(transform.position, context.direction));
    }

    protected override void ApplyStat(ReactiveGroundStat current)
    {
        stat = current.Clone();
        stat.Clamp();
        if (visual != null && scaleVisual)
            visual.localScale = Vector3.Scale(visualScale, new Vector3(stat.groundRadius * 2f, stat.groundRadius * 2f, 1f));
    }

    private void Update()
    {
        if (!initialized || finishing) return;
        if (!context.CanContinue) { Clear(); return; }
        elapsed += Time.deltaTime;
        if (elapsed >= stat.groundLifetime)
        {
            finishing = true;
            active.Remove(this);
            CompleteLifetime();
            Destroy(gameObject);
            return;
        }
        tickElapsed += Time.deltaTime;
        if (tickElapsed < stat.groundTickInterval) return;
        tickElapsed = 0f;
        ItemEffectUtility.Execute(IsSpecialActive ? specials : defaults,
            context.Copy(transform.position, context.direction));
    }

    // 착지 전에 영역 목록을 복사한다. 이번 착지로 새로 생성된 장판은 같은 착지에 반응하지 않는다.
    public static bool NotifyItemLanded(ItemEffectContext incoming)
    {
        if (incoming == null || !incoming.CanContinue) return false;
        bool replace = false;
        foreach (ReactiveGroundArea area in active.ToArray())
        {
            if (!incoming.CanContinue) break;
            if (area != null && area.TryReact(incoming)) replace |= area.replaceIncoming;
        }
        return replace;
    }

    private bool TryReact(ItemEffectContext incoming)
    {
        if (!initialized || finishing || reacting || !isActiveAndEnabled || !context.CanContinue ||
            elapsed >= stat.groundLifetime || Time.time < nextReaction) return false;
        if (sameOwner && incoming.owner != owner) return false;
        if (((Vector2)(incoming.targetPosition - transform.position)).sqrMagnitude > stat.groundRadius * stat.groundRadius)
            return false;
        if (accepted != null && accepted.Length > 0 && System.Array.IndexOf(accepted, incoming.sourceItemData) < 0)
            return false;
        nextReaction = Time.time + stat.groundReactionCooldown;
        specialUntil = Time.time + stat.groundSpecialDuration;
        tickElapsed = 0f;
        reacting = true;
        try
        {
            // 피해/버프의 출처는 장판 생성자다. 착지 아이템은 acceptedItems 필터에 사용한다.
            ItemEffectUtility.Execute(specials, context.Copy(transform.position, incoming.direction));
        }
        finally { reacting = false; }
        return true;
    }

    protected override void OnDisable()
    {
        initialized = false;
        active.Remove(this);
        base.OnDisable();
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (stat != null) Gizmos.DrawWireSphere(transform.position, stat.groundRadius);
    }
#endif
}
