using UnityEngine;

public enum StoryProgressStartMode { Confirmation, Delay }

[CreateAssetMenu(menuName = "Story/Progression/Rule")]
public sealed class StoryProgressRule : ScriptableObject
{
    [Tooltip("Unique, permanent save ID. Changing this starts a separate progress record.")]
    public string id = System.Guid.NewGuid().ToString("N");
    public StoryData story;
    public StoryProgressCondition condition;
    [Header("Start after the condition is met")]
    public StoryProgressStartMode startMode;
    [Tooltip("battle-result / potion-result / chest-result, or your own confirmation area ID.")]
    public string confirmationArea = StoryProgressManager.BattleResult;
    [Min(0f)] public float delaySeconds = 3f;
    public string returnScene = GameSceneRoutes.Upgrade;
}
