using System.Collections.Generic;

// 등록 시 분류를 복사한다. 실행 중 정의 에셋이 바뀌어도 재부여 전까지 유지한다.
public static class BuffClassificationUtility
{
    public static void Capture(ItemEffectData source, out bool harmful, out bool dispellable,
        out BuffFlagDefinition[] flags)
    {
        BuffEffect buff = source as BuffEffect;
        GrantSpecialStatusEffect status = source as GrantSpecialStatusEffect;
        BuffClassificationDefinition definition = buff != null ? buff.buffClassification :
            (status != null ? status.buffClassification : null);
        harmful = (definition != null && definition.harmful) || (buff != null && buff.harmful);
        dispellable = (definition == null || definition.dispellable) && (buff == null || buff.dispellable);
        List<BuffFlagDefinition> tags = new List<BuffFlagDefinition>();
        AddTags(tags, definition != null ? definition.flags : null);
        AddTags(tags, buff != null ? buff.flags : null);
        flags = tags.Count > 0 ? tags.ToArray() : null;
    }

    private static void AddTags(List<BuffFlagDefinition> result, BuffFlagDefinition[] source)
    {
        if (source == null) return;
        foreach (BuffFlagDefinition tag in source)
            if (tag != null && !result.Contains(tag)) result.Add(tag);
    }
}
