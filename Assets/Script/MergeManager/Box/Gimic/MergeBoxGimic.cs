using System;
using UnityEngine;

public abstract class MergeBoxGimic : MonoBehaviour
{
    [Serializable]
    public class GimicSkill
    {
        public string id;
        public Sprite skillImage;
        [Min(0f)] public float defaultSkillCool = 10f;
    }

    protected MergegameManager Board { get; private set; }
    private GimicSkillRuntime[] runtimeSkills = System.Array.Empty<GimicSkillRuntime>();
    public ItemCooldownStat GetSkillStat(int index) => index >= 0 && index < runtimeSkills.Length ? runtimeSkills[index].Resolve() : new ItemCooldownStat();
    protected GimicData Data { get; private set; }
    public bool Running { get; private set; }
    public GimicSkill[] skills = System.Array.Empty<GimicSkill>();
    public virtual int SkillCount => skills.Length;
    public Sprite GetSkillSprite(int index) => index >= 0 && index < skills.Length ? skills[index]?.skillImage : null;
    public void Init(GimicData data) => Data = data;
    public void Initialize(MergegameManager board)
    {
        GimicStop();
        Board = board;
        EnsureSkills();
        runtimeSkills = new GimicSkillRuntime[skills.Length];
        var ids = new System.Collections.Generic.HashSet<string>();
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] != null && string.IsNullOrWhiteSpace(skills[i].id)) skills[i].id = board.itemSeries + ".Skill" + (i + 1);
            if (skills[i] == null || !ids.Add(skills[i].id))
            {
                Debug.LogError("기믹 스킬 ID는 비어 있지 않은 고유 값이어야 합니다.", this);
                GimicStop(); return;
            }
            runtimeSkills[i] = ScriptableObject.CreateInstance<GimicSkillRuntime>();
            runtimeSkills[i].Initialize(skills[i]);
        }
        GimicStart();
        Running = true;
    }
    protected virtual void EnsureSkills() { }
    public virtual void GimicStart() { }
    public virtual void GimicStop()
    {
        Running = false;
        foreach (var skill in runtimeSkills) if (skill != null)
        {
            skill.Shutdown();
            if (Application.isPlaying) Destroy(skill); else DestroyImmediate(skill);
        }
        runtimeSkills = System.Array.Empty<GimicSkillRuntime>();
    }
    public virtual void UseBoxSkill(int index) { }
    public virtual float GetSkillCooldown(int index) => index >= 0 && index < skills.Length && skills[index] != null ? Mathf.Max(0f, skills[index].defaultSkillCool) : 0f;
    protected virtual void OnDisable() => GimicStop();
}

public class GimicData { }
public class WeaponGimicData : GimicData { }
public class FoodGimicData : GimicData { }
