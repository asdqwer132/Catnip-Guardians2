using UnityEngine;

// 스킬마다 별도 타깃 객체를 만들어 같은 버튼을 사용해도 버프가 섞이지 않습니다.
public sealed class GimicSkillRuntime : ScriptableObject, IBuffTarget
{
    private GimicSkill definition;
    private BuffManager manager;
    public Object BuffTargetObject => this;
    public string BuffTargetGroup => "MergeSkill/" + definition.id;
    public string BuffTargetDebugName => definition.id;
    public ItemCooldownStat Stat { get; private set; }
    public void Initialize(GimicSkill skill)
    {
        definition = skill;
        RefreshBuffedStat();
    }
    public void RefreshBuffedStat()
    {
        var source = new ItemCooldownStat { cooldown = definition.cooldown };
        Stat = manager != null ? manager.GetBuffedStatForTarget(source, this, BuffCalculationMode.All, false) : source;
        Stat.Clamp();
    }
    public ItemCooldownStat Resolve()
    {
        if (manager != BuffManager.instance)
        {
            if (manager != null) manager.UnregisterBuffTarget(this);
            manager = BuffManager.instance;
            if (manager != null) manager.RegisterBuffTarget(this);
        }
        RefreshBuffedStat();
        return Stat;
    }
    public void Shutdown() { if (manager != null) manager.UnregisterBuffTarget(this); manager = null; }
    private void OnDestroy() { if (manager != null) manager.UnregisterBuffTarget(this); }
}
