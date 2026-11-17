using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class MergeBoxFishing : MergeBoxGimic
{
    [Header("Fish rewards")]
    public Collider2D fishDropArea;
    public WeightedMergeItem[] fish = System.Array.Empty<WeightedMergeItem>();
    [Min(1)] public int minimumFish = 6, maximumFish = 12;
    [Header("Timing minigame · normalized 0..1")]
    [Min(.01f)] public float needleSpeed = 1f;
    [Min(.1f)] public float timeLimit = 5f;
    public Vector2 successRange = new Vector2(.4f, .6f);
    [Header("Authored UI · optional")]
    public GameObject minigamePanel;
    public Slider needle;
    public RectTransform successIndicator;
    public Button catchButton;
    public UnityEvent<bool> fishingFinished = new UnityEvent<bool>();
    public bool IsFishing { get; private set; }
    public float NeedleValue { get; private set; }
    public Color catchWindowColor = Color.green;
    private float elapsed;
    public bool IsInCatchWindow => NeedleValue >= Mathf.Clamp01(Mathf.Min(successRange.x, successRange.y)) && NeedleValue <= Mathf.Clamp01(Mathf.Max(successRange.x, successRange.y));
    public override float GetSkillDisplayProgress(int index) => IsFishing && IsActiveSkill(index) ? NeedleValue : base.GetSkillDisplayProgress(index);
    public override Color? GetSkillDisplayColor(int index) => IsFishing && IsActiveSkill(index) && IsInCatchWindow ? catchWindowColor : (Color?)null;
    protected override void EnsureSkills() => SetDefaultSkills(
        new GimicSkill { id = "Fishing.Begin", type = GimicSkillType.Passive, defaultSkillCool = 15f },
        new GimicSkill { id = "Fishing.Catch", type = GimicSkillType.Active, defaultSkillCool = 0f });
    public override void GimicStart()
    {
        IsFishing = false; elapsed = 0f; NeedleValue = 0f;
        if (catchButton != null) catchButton.onClick.AddListener(SubmitCatch);
        RefreshUI();
    }
    protected override bool CanUseActiveSkill(int index) => IsFishing;
    protected override void OnActiveSkill(int index) => SubmitCatch();
    protected override void OnPassiveSkill(int index) => BeginFishing();
    public void BeginFishing()
    {
        if (!Running || Board == null || !Board.CanOperate || IsFishing || Board.PendingSpawnCount > 0) return;
        IsFishing = true; elapsed = 0f; NeedleValue = 0f; RefreshUI();
    }
    public void SubmitCatch()
    {
        if (!Running || Board == null || !Board.CanOperate || !IsFishing) return;
        float min = Mathf.Clamp01(Mathf.Min(successRange.x, successRange.y));
        float max = Mathf.Clamp01(Mathf.Max(successRange.x, successRange.y));
        Finish(NeedleValue >= min && NeedleValue <= max);
    }
    protected override void TickGimic(float deltaTime)
    {
        if (!IsFishing) return;
        elapsed += deltaTime;
        NeedleValue = Mathf.PingPong(elapsed * Mathf.Max(.01f, needleSpeed), 1f);
        RefreshUI();
        if (elapsed >= Mathf.Max(.1f, timeLimit)) Finish(false);
    }
    private void Finish(bool success)
    {
        IsFishing = false;
        if (success && Board is FishingMergeGameManager fishing)
            fishing.DropFish(fish, Random.Range(Mathf.Max(1, minimumFish), Mathf.Max(minimumFish, maximumFish) + 1), fishDropArea);
        RefreshUI(); fishingFinished.Invoke(success);
    }
    private void RefreshUI()
    {
        if (minigamePanel != null) minigamePanel.SetActive(IsFishing);
        if (catchButton != null) catchButton.interactable = IsFishing;
        if (needle != null) needle.normalizedValue = NeedleValue;
        if (successIndicator != null)
        {
            var min = successIndicator.anchorMin; var max = successIndicator.anchorMax;
            min.x = Mathf.Clamp01(Mathf.Min(successRange.x, successRange.y)); max.x = Mathf.Clamp01(Mathf.Max(successRange.x, successRange.y));
            successIndicator.anchorMin = min; successIndicator.anchorMax = max;
            var low = successIndicator.offsetMin; var high = successIndicator.offsetMax; low.x = high.x = 0f;
            successIndicator.offsetMin = low; successIndicator.offsetMax = high;
        }
    }
    public override void GimicStop()
    {
        if (catchButton != null) catchButton.onClick.RemoveListener(SubmitCatch);
        IsFishing = false; RefreshUI(); base.GimicStop();
    }
}
