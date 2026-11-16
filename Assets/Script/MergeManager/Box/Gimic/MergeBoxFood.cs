using System.Collections.Generic;
using UnityEngine;

public sealed class MergeBoxFood : MergeBoxGimic
{
    public Collider2D burningArea;
    [Min(0.01f)] public float bakeInterval = 5f;
    [Min(1)] public int burnsBeforeDestroyed = 3;
    [HideInInspector, Min(0f)] public float skillCooldown = 10f;
    private float nextBake;
    private readonly List<Collider2D> overlaps = new List<Collider2D>();
    private readonly HashSet<FoodMergeItemObject> baked = new HashSet<FoodMergeItemObject>();
    protected override void EnsureSkills()
    {
        if (skills.Length == 0) skills = new[] { new GimicSkill { id = "Food.Bake", defaultSkillCool = skillCooldown } };
    }
    public override int SkillCount => skills.Length > 0 ? skills.Length : 1;
    public override float GetSkillCooldown(int index) => skills.Length > 0 ? base.GetSkillCooldown(index) : index == 0 ? skillCooldown : 0f;
    public override void GimicStart() => nextBake = Time.time + Mathf.Max(.01f, bakeInterval);
    private void Update()
    {
        if (!Running || Board == null || !Board.CanOperate || Time.time < nextBake) return;
        nextBake = Time.time + Mathf.Max(.01f, bakeInterval);
        Bake();
    }
    public override void UseBoxSkill(int index)
    {
        if (index == 0) Bake();
    }
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
