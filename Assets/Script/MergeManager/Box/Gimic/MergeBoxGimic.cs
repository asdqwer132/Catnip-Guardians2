using System;
using UnityEngine;

public enum GimicSkillType { Active, Passive }

public abstract class MergeBoxGimic : MonoBehaviour
{
    [Serializable]
    public class GimicSkill
    {
        public string id;
        [Tooltip("Active: 클릭/단축키로 발동. Passive: 직접 입력 없이 발동합니다.")]
        public GimicSkillType type;
        public Sprite skillImage;
        [Tooltip("액티브는 사용 후 대기 시간, 패시브는 반복 발동 주기입니다. 0인 패시브는 타이머를 사용하지 않습니다.")]
        [Min(0f)] public float defaultSkillCool = 10f;
    }

    protected MergegameManager Board { get; private set; }
    private GimicSkillRuntime[] runtimeSkills = System.Array.Empty<GimicSkillRuntime>();
    public ItemCooldownStat GetSkillStat(int index) => index >= 0 && index < runtimeSkills.Length ? runtimeSkills[index].Resolve() : new ItemCooldownStat();
    private float[] remaining = Array.Empty<float>();
    private bool[] passiveTimerStarted = Array.Empty<bool>();
    public bool IsActiveSkill(int index) => index >= 0 && index < skills.Length && skills[index] != null && skills[index].type == GimicSkillType.Active;
    public float GetSkillProgress(int index) => index >= 0 && index < remaining.Length ? remaining[index] : 0f;
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
        remaining = new float[skills.Length];
        passiveTimerStarted = new bool[skills.Length];
        for (int i = 0; i < skills.Length; i++)
            if (!IsActiveSkill(i) && GetSkillStat(i).cooldown > 0f)
            { remaining[i] = 1f; passiveTimerStarted[i] = true; }
        GimicStart();
        Running = true;
    }
    protected virtual void EnsureSkills() { }
    public virtual void GimicStart() { }
    public virtual void GimicStop()
    {
        Running = false;
        remaining = Array.Empty<float>();
        passiveTimerStarted = Array.Empty<bool>();
        foreach (var skill in runtimeSkills) if (skill != null)
        {
            skill.Shutdown();
            if (Application.isPlaying) Destroy(skill); else DestroyImmediate(skill);
        }
        runtimeSkills = System.Array.Empty<GimicSkillRuntime>();
    }
    // UI/키 입력은 반드시 이 진입점을 사용합니다. 패시브는 직접 사용할 수 없습니다.
    public void UseBoxSkill(int index) => TryUseActiveSkill(index);
    public bool TryUseActiveSkill(int index)
    {
        if (!Running || Board == null || !Board.CanOperate || !IsActiveSkill(index) || remaining[index] > 0f) return false;
        remaining[index] = GetSkillStat(index).cooldown > 0f ? 1f : 0f;
        OnActiveSkill(index);
        return true;
    }
    // 게임 매니저가 한 번씩 호출합니다. 상자별 Update에서 타이머를 중복 실행하지 않습니다.
    public void TickSkills(float deltaTime)
    {
        if (!Running || Board == null || !Board.CanOperate || deltaTime <= 0f) return;
        for (int i = 0; i < remaining.Length; i++)
        {
            var stat = GetSkillStat(i);
            bool passive = !IsActiveSkill(i);
            if (stat.cooldown <= 0f)
            { remaining[i] = 0f; passiveTimerStarted[i] = false; continue; }
            if (passive && !passiveTimerStarted[i])
            { remaining[i] = 1f; passiveTimerStarted[i] = true; }
            bool wasCooling = remaining[i] > 0f;
            remaining[i] = Mathf.Max(0f, remaining[i] - deltaTime * stat.cooldownRecoveryRate / stat.cooldown);
            if (passive && wasCooling && remaining[i] <= 0f)
            {
                remaining[i] = 1f;
                OnPassiveSkill(i);
                if (!Running) break;
            }
        }
    }
    // 새 상자는 발동 내용만 구현합니다. 쿨이 없는 지속/이벤트 패시브는 GimicStart 또는 자체 이벤트에서 처리합니다.
    protected virtual void OnActiveSkill(int index) { }
    protected virtual void OnPassiveSkill(int index) { }
    public virtual float GetSkillCooldown(int index) => index >= 0 && index < runtimeSkills.Length ? GetSkillStat(index).cooldown : index >= 0 && index < skills.Length && skills[index] != null ? Mathf.Max(0f, skills[index].defaultSkillCool) : 0f;
    protected virtual void OnDisable() => GimicStop();
}

public class GimicData { }
public class WeaponGimicData : GimicData { }
public class FoodGimicData : GimicData { }
