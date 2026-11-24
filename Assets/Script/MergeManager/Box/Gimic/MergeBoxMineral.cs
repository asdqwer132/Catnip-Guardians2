using UnityEngine;

public sealed class MergeBoxMineral : MergeBoxGimic
{
    public Transform cart;
    public Transform cartStart, cartEnd;
    public SpriteRenderer cartRenderer;
    public Sprite[] loadedFrames = System.Array.Empty<Sprite>();
    public Sprite[] pouringFrames = System.Array.Empty<Sprite>();
    public Sprite[] emptyFrames = System.Array.Empty<Sprite>();
    [Min(.01f)] public float cartSpeed = 4f;
    [Min(0f)] public float pourSeconds = 2f;
    [Min(.01f)] public float animationFrameSeconds = .15f;
    private int phase, activeRequest = -1;
    private float elapsed, animationTime;
    private Vector3 deliveryPosition;
    public Collider2D oreSpawnArea;
    public WeightedMergeItem[] ores = System.Array.Empty<WeightedMergeItem>();
    [Min(1)] public int maximumOres = 5;
    protected override void EnsureSkills()
    {
        SetDefaultSkills(new GimicSkill { id = "Mineral.SpawnOre", type = GimicSkillType.Passive, defaultSkillCool = 8f });
        if (skills.Length == 1 && skills[0].id == "Mineral.SpawnOre")
        {
            var passive = skills[0];
            skills = new[] { passive, new GimicSkill { id = "Mineral.SpawnOre.Active", type = GimicSkillType.Active, defaultSkillCool = 10f, skillImage = passive.skillImage } };
        }
    }
    protected override bool CanUseActiveSkill(int index) => phase == 0;
    protected override bool WaitForSkillCompletion(int index) => true;
    protected override void OnActiveSkill(int index) => BeginDelivery(index);
    protected override void OnPassiveSkill(int index)
    { if (phase == 0) BeginDelivery(-1); }
    private void BeginDelivery(int index)
    {
        if (!(Board is { } mineral) || mineral.OreCount >= maximumOres || oreSpawnArea == null)
        { if (index >= 0) CompleteSkill(index); return; }
        activeRequest = index;
        if (!MergeGimmickUtility.TryPoint(oreSpawnArea, out deliveryPosition))
        { if (index >= 0) CompleteSkill(index); activeRequest = -1; return; }
        elapsed = animationTime = 0f;
        if (cart != null && cartStart != null) cart.position = cartStart.position;
        phase = cart != null ? 1 : 2;
    }
    private void Animate(Sprite[] frames, float delta)
    {
        animationTime += delta;
        if (cartRenderer != null && frames != null && frames.Length > 0)
            cartRenderer.sprite = frames[Mathf.FloorToInt(animationTime / Mathf.Max(.01f, animationFrameSeconds)) % frames.Length];
    }
    protected override void TickGimic(float deltaTime)
    {
        if (phase == 1 || phase == 3)
        {
            Vector3 target = phase == 1 ? deliveryPosition : cartEnd != null ? cartEnd.position : cart.position;
            cart.position = Vector3.MoveTowards(cart.position, target, Mathf.Max(.01f, cartSpeed) * deltaTime);
            Animate(phase == 1 ? loadedFrames : emptyFrames, deltaTime);
            if ((cart.position - target).sqrMagnitude < .000001f)
            {
                if (phase == 1) { phase = 2; elapsed = animationTime = 0f; }
                else { phase = 0; if (activeRequest >= 0) CompleteSkill(activeRequest); activeRequest = -1; }
            }
        }
        else if (phase == 2)
        {
            elapsed += deltaTime; Animate(pouringFrames, deltaTime);
            if (elapsed >= pourSeconds && Board is { } mineral)
            {
                mineral.TrySpawnOre(ores, oreSpawnArea, maximumOres, deliveryPosition);
                phase = cart != null ? 3 : 0;
                if (phase == 0 && activeRequest >= 0) { CompleteSkill(activeRequest); activeRequest = -1; }
            }
        }
    }
    public override void GimicStop()
    { phase = 0; activeRequest = -1; if (cart != null && cartStart != null) cart.position = cartStart.position; if (Board is { } mineral) mineral.ClearOres(); base.GimicStop(); }
}
