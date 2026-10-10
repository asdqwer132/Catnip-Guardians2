using System;
using UnityEngine;

// Presentation settings for effects whose lifetime is owned by a gameplay runner.
[Serializable]
public sealed class EffectBuffUISettings
{
    [Tooltip("켜면 효과가 유지되는 동안 기존 버프 UI에 표시합니다. 효과의 동작과 중첩 규칙은 변경하지 않습니다.")]
    public bool showInUI;
    [Tooltip("비우면 사용한 아이템 이름, 아이템이 없으면 효과 에셋 이름을 표시합니다.")]
    public string displayName;
    [Tooltip("비우면 사용한 아이템의 아이콘을 표시합니다.")]
    public Sprite buffIcon;

    public EffectBuffUIHandle Register(ItemEffectData effect, ItemEffectContext context,
        IBuffTarget target, float duration)
    {
        if (!showInUI || context == null) return null;
        BuffManager manager = context.buffManager != null ? context.buffManager : BuffManager.instance;
        return manager != null
            ? manager.RegisterEffectUI(effect, context, target, duration, displayName, buffIcon) : null;
    }
}
