using UnityEngine;

public abstract class MergeBoxGimic : MonoBehaviour
{
    protected MergegameManager Board { get; private set; }
    protected GimicData Data { get; private set; }
    public bool Running { get; private set; }
    public virtual int SkillCount => 0;
    public void Init(GimicData data) => Data = data;
    public void Initialize(MergegameManager board)
    {
        GimicStop();
        Board = board;
        GimicStart();
        Running = true;
    }
    public virtual void GimicStart() { }
    public virtual void GimicStop() => Running = false;
    public virtual void UseBoxSkill(int index) { }
    public virtual float GetSkillCooldown(int index) => 0f;
    protected virtual void OnDisable() => GimicStop();
}

public class GimicData { }
public class WeaponGimicData : GimicData { }
public class FoodGimicData : GimicData { }
