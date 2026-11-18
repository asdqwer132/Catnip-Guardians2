using UnityEngine;

public sealed class MergeBoxFishing : MergeBoxGimic
{
    [Header("기존 미니게임 연결")]
    public FishingMiniGame minigame;
    public GameObject minigamePanel;
    [Header("성공 보상")]
    public Collider2D fishDropArea;
    public WeightedMergeItem[] fish = System.Array.Empty<WeightedMergeItem>();
    [Min(1)] public int minimumFish = 6, maximumFish = 12;
    [Header("대성공 보상 · 성공 보상을 대신합니다")]
    public WeightedMergeItem[] perfectFish = System.Array.Empty<WeightedMergeItem>();
    [Min(1)] public int minimumPerfectFish = 12, maximumPerfectFish = 24;
    public bool IsFishing { get; private set; }

    protected override void EnsureSkills()
    {
        var old=skills!=null && skills.Length>0?skills[0]:null;
        skills=new[] { new GimicSkill { id="Fishing.Begin",type=GimicSkillType.Active,
            skillImage=old!=null?old.skillImage:null, defaultSkillCool=old!=null?old.defaultSkillCool:15f,
            enabled=old==null || old.enabled,
            activationPresentation=old!=null?old.activationPresentation:new EffectPresentationCue(),
            completionPresentation=old!=null?old.completionPresentation:new EffectPresentationCue() } };
    }
    protected override bool CanUseActiveSkill(int index) => minigame!=null && !IsFishing;
    protected override bool WaitForSkillCompletion(int index) => true;
    protected override void OnActiveSkill(int index) => StartFishing();
    public override void GimicStart() { IsFishing=false;minigame?.StopMiniGame();if(minigamePanel!=null) minigamePanel.SetActive(false); }
    public void BeginFishing() => TryUseActiveSkill(0);
    private void StartFishing()
    {
        if(!Running || Board==null || !Board.CanOperate || IsFishing || minigame==null) return;
        IsFishing=true;
        if(minigamePanel!=null) minigamePanel.SetActive(true);
        minigame.StartMiniGame();
    }
    // FishingMiniGame의 Inspector 이벤트에 연결합니다. 한 판에 한 번만 처리합니다.
    public void OnFishingFailed() => Finish(0);
    public void OnFishingSuccess() => Finish(1);
    public void OnFishingPerfect() => Finish(2);
    private void Finish(int result)
    {
        if(!Running || Board==null || !Board.CanOperate || !IsFishing) return;
        IsFishing=false;
        minigame?.StopMiniGame();
        if(minigamePanel!=null) minigamePanel.SetActive(false);
        CompleteSkill(0);
        if(result==0) { Debug.Log("[Fishing] 미니게임 실패",this);return; }
        if(!(Board is FishingMergeGameManager fishing)) return;
        bool perfect=result==2;
        var rewards=perfect && perfectFish!=null && perfectFish.Length>0?perfectFish:fish;
        int min=Mathf.Max(1,perfect?minimumPerfectFish:minimumFish);
        int max=Mathf.Max(min,perfect?maximumPerfectFish:maximumFish);
        fishing.DropFish(rewards,Random.Range(min,max+1),fishDropArea);
    }
    public override void GimicStop()
    {
        if(IsFishing) { minigame?.StopMiniGame();if(minigamePanel!=null) minigamePanel.SetActive(false); }
        IsFishing=false;base.GimicStop();
    }
}
