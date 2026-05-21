using UnityEngine;

public abstract class SkillRewardData : ScriptableObject
{
    public abstract void Apply(SkillApplyContext context);

    public virtual void Remove(SkillApplyContext context) { }
}