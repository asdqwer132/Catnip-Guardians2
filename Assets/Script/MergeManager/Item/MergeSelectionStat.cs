using System;

[Serializable]
public sealed class MergeSelectionStat : IGameStat<MergeSelectionStat>
{
    public float selectionWidth = 3f;
    public float selectionHeight = 3f;
    public float selectionDragThreshold = .1f;
    public float selectionDragCooldown = .5f;
    public float selectionClickCooldown = .2f;
    public MergeSelectionStat Clone() => (MergeSelectionStat)MemberwiseClone();
    public void Clamp()
    {
        selectionWidth=EffectStatUtility.Safe(selectionWidth,.01f,100000f,3f);
        selectionHeight=EffectStatUtility.Safe(selectionHeight,.01f,100000f,3f);
        selectionDragThreshold=EffectStatUtility.Safe(selectionDragThreshold,.001f,100000f,.1f);
        selectionDragCooldown=EffectStatUtility.Safe(selectionDragCooldown,0f,600f,.5f);
        selectionClickCooldown=EffectStatUtility.Safe(selectionClickCooldown,0f,600f,.2f);
    }
}
