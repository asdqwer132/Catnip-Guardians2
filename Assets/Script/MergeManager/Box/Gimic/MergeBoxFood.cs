using System.Collections.Generic;
using UnityEngine;

public sealed class MergeBoxFood : MergeBoxGimic
{
    public Collider2D burningArea;
    [HideInInspector, Min(0.01f)] public float bakeInterval = 5f;
    [Min(1)] public int burnsBeforeDestroyed = 3;
    [HideInInspector, Min(0f)] public float skillCooldown = 10f;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<FoodMergeItemObject> baked = new HashSet<FoodMergeItemObject>();
    protected override void EnsureSkills()
    {
        if (skills == null || skills.Length == 0)
            skills = new[] { new GimicSkill { id = "Food.Bake", type = GimicSkillType.Active, defaultSkillCool = skillCooldown } };
        // 기존 액티브 설정과 ID를 보존하면서 자동 굽기를 별도 패시브로 이관합니다.
        if (!System.Array.Exists(skills, skill => skill != null && skill.type == GimicSkillType.Passive))
        {
            var list = new List<GimicSkill>(skills);
            list.Add(new GimicSkill { id = "Food.AutoBake", type = GimicSkillType.Passive, defaultSkillCool = bakeInterval });
            skills = list.ToArray();
        }
    }
    protected override void OnActiveSkill(int index) => Bake();
    protected override void OnPassiveSkill(int index) => Bake();

    public void Bake()
    {
        if (!Running || Board == null || !Board.CanOperate || burningArea == null || !burningArea.isActiveAndEnabled) return;
        overlaps.Clear(); baked.Clear();
        // 굽는 순간 실제 콜라이더가 겹친 아이템만 검사합니다. 다중 콜라이더도 한 번만 굽습니다.
        Physics2D.SyncTransforms();
        var filter = new ContactFilter2D { useTriggers = true, useLayerMask = false };
        burningArea.Overlap(filter, overlaps);
        foreach (var hit in overlaps)
        {
            var food = hit != null ? hit.GetComponentInParent<FoodMergeItemObject>() : null;
            if (food != null && food.Owner == Board && baked.Add(food)) food.Bake(burnsBeforeDestroyed);
        }
    }
}
