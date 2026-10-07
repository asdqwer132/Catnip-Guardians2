using UnityEngine;

public enum SequenceAttackMode { ForwardBounce, ScatterBombs, RepeatAtPoint }
public enum BombTriggerMode { OnArrival, AfterDelay, EnemyNearbyOrTimeout }

[CreateAssetMenu(fileName = "SequenceAttackEffect", menuName = "GameData/Items/Effects/Attack/Sequence Attack")]
public class SequenceAttackEffect : ItemEffectData
{
    [Header("Sequence")]
    public SequenceAttackMode mode = SequenceAttackMode.ForwardBounce;
    public SequenceAttackStat sequenceStat = new SequenceAttackStat();
    public AttackDirectionMode directionMode;
    public Vector2 fixedWorldDirection = Vector2.right;
    [Header("Effects")]
    [Tooltip("연속 공격 시작점에서 한 번 실행할 효과. 분산 전 최초 폭발 등을 연결합니다.")]
    public ItemEffectData[] onStartEffects;
    [Tooltip("매 착지/폭탄 폭발에서 실행할 효과입니다.")]
    public ItemEffectData[] onImpactEffects;
    [Tooltip("마지막 착지/모든 폭탄 폭발 이후 실행할 효과. 다른 Sequence를 연결할 수도 있습니다.")]
    public ItemEffectData[] afterLastImpactEffects;
    [Header("Flight")]
    [Tooltip("없으면 기본 투사체를 자동 생성합니다. 연속 공격은 별도 사거리 표시를 생성하지 않습니다.")]
    public ItemThrowMover projectilePrefab;
    public Sprite projectileSprite;
    [Header("Scatter Bombs")]
    public bool randomScatter;
    public BombTriggerMode bombTrigger;
    public LayerMask enemyLayerMask = ~0;

    public override void Prepare(ItemEffectContext context)
    {
        context.GetSnapshotStat(this, sequenceStat);
        PrepareEffects(context, onStartEffects);
        PrepareEffects(context, onImpactEffects);
        PrepareEffects(context, afterLastImpactEffects);
    }

    private static void PrepareEffects(ItemEffectContext context, ItemEffectData[] effects)
    {
        if (effects == null) return;
        for (int i = 0; i < effects.Length; i++)
            if (effects[i] != null && effects[i].CanExecute(context))
                context.plan.Prepare(effects[i], context);
    }

    public override void ExecuteEffect(ItemEffectContext context)
    {
        if (context == null || sequenceStat == null) return;
        GameObject host = new GameObject("SequenceAttack");
        SequenceAttackRunner runner = host.AddComponent<SequenceAttackRunner>();
        if (ItemRuntimeObjectManager.Instance != null) ItemRuntimeObjectManager.Instance.Register(host);
        runner.Init(this, context);
    }
}
