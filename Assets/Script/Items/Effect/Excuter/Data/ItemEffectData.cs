using UnityEngine;

public abstract class ItemEffectData : ScriptableObject
{
    [Header("Execution Conditions")]
    [Tooltip("All: 모든 조건 만족 / Any: 하나 이상 만족. 조건 목록이 비어 있으면 항상 실행합니다.")]
    public ItemEffectConditionMode conditionMode = ItemEffectConditionMode.All;
    public ItemEffectConditionData[] conditions;

    [Header("Visual / Audio")]
    [Tooltip("공유 연출 에셋. 아이템 효과와 명중 효과에 같은 에셋을 연결할 수 있습니다.")]
    public EffectVisualData visualData;

    // 기존 에셋의 직렬화 필드 이름을 유지한다. 새 설정은 visualData에서 관리한다.
    // Editor의 이전 메뉴로 옮기기 전까지는 기존 연출을 계속 사용할 수 있다.
    [HideInInspector] public ImpactVfxInstance impactVfxPrefab;
    [HideInInspector] public Vector3 impactBaseScale = Vector3.one;
    [HideInInspector] public bool scaleImpactVfxByRadius = true;
    [HideInInspector] public bool useAnimatorClipLifeTime = true;
    [HideInInspector] public string audioSource;
    [HideInInspector] public float impactVfxLifeTime = 1f;

    public bool HasLegacyVisualSettings =>
        impactVfxPrefab != null || !string.IsNullOrWhiteSpace(audioSource);

    public void Execute(ItemEffectContext context)
    {
        if (context == null)
            return;

        // 분기 에셋을 서로 연결한 경우에도 같은 실행 경로의 순환 참조를 차단한다.
        if (!context.TryBeginEffectExecution(this))
        {
            Debug.LogWarning("ItemEffectData: 이펙트 실행 중 순환 참조가 발견되어 실행을 건너뜁니다.", this);
            return;
        }

        ItemEffectData previousEffect = context.currentEffectData;
        context.SetCurrentEffect(this);
        try
        {
            ExecuteWithConditions(context);
        }
        finally
        {
            context.SetCurrentEffect(previousEffect);
            context.EndEffectExecution(this);
        }
    }

    protected virtual void ExecuteWithConditions(ItemEffectContext context)
    {
        if (!AreConditionsSatisfied(context))
            return;

        PlayImpactVfx(context);
        ExecuteEffect(context);
    }

    // 조건 조회는 연출, 피해, 버프 횟수 소비 없이 조건 충족 여부만 확인한다.
    public bool AreConditionsSatisfied(ItemEffectContext context)
    {
        if (context == null)
            return false;

        // 기존 이펙트 에셋은 조건이 없으므로 기존 동작을 유지한다.
        if (conditions == null || conditions.Length == 0)
            return true;

        switch (conditionMode)
        {
            case ItemEffectConditionMode.All:
                for (int i = 0; i < conditions.Length; i++)
                {
                    ItemEffectConditionData condition = conditions[i];
                    if (condition == null || !condition.IsSatisfied(context))
                        return false;
                }
                return true;

            case ItemEffectConditionMode.Any:
                for (int i = 0; i < conditions.Length; i++)
                {
                    ItemEffectConditionData condition = conditions[i];
                    if (condition != null && condition.IsSatisfied(context))
                        return true;
                }
                return false;

            default:
                return false;
        }
    }

    public abstract void ExecuteEffect(ItemEffectContext context);

    #region Impact
    protected virtual void PlayImpactVfx(ItemEffectContext context)
    {
        if (context == null || (visualData == null && !HasLegacyVisualSettings))
            return;

        Quaternion rotation;
        if (!TryGetImpactRotation(context, out rotation))
            return;

        Vector3 position = context.targetPosition;
        position.z = 0f;

        // Executor의 공유 컨텍스트가 다음 이펙트에서 바뀌어도 이번 연출은 영향을 받지 않는다.
        ItemEffectContext snapshot = new ItemEffectContext(
            context.owner, context.sourceItemData, context.usePosition,
            context.targetPosition, context.sourceBag, this, context.buffManager,
            context.direction
        );

        EffectVisualContext visualContext = new EffectVisualContext(
            position,
            rotation,
            scaleResolver: (baseScale, useRadiusScale) =>
                GetCurrentImpactScale(snapshot, baseScale, useRadiusScale)
        );

        if (visualData != null)
            visualData.Play(visualContext);
        else
            EffectVisualData.PlayLegacy(
                visualContext, impactVfxPrefab, impactBaseScale,
                scaleImpactVfxByRadius, impactVfxLifeTime,
                useAnimatorClipLifeTime, audioSource
            );
    }

    protected virtual bool TryGetImpactRotation(ItemEffectContext context, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        return context != null;
    }

    // 이전 도구는 새 공유 에셋을 저장한 뒤에만 이 메서드를 호출한다.
    public void ClearLegacyVisualSettings()
    {
        impactVfxPrefab = null;
        impactBaseScale = Vector3.one;
        scaleImpactVfxByRadius = true;
        useAnimatorClipLifeTime = true;
        audioSource = null;
        impactVfxLifeTime = 1f;
    }
    public Vector3 GetCurrentImpactScale(ItemEffectContext context, Vector3 baseScale, bool useRadiusScale)
    {
        Vector3 finalScale = baseScale;

        if (useRadiusScale)
        {
            float radius = GetImpactRadius(context);
            radius = Mathf.Max(0.01f, radius);

            float diameter = radius * 2f;

            finalScale = Vector3.Scale(finalScale, new Vector3(diameter, diameter, 1f));
        }

        Vector3 additionalScale = GetAdditionalImpactScale(context);
        finalScale = Vector3.Scale(finalScale, additionalScale);

        return finalScale;
    }
    protected virtual float GetImpactRadius(ItemEffectContext context) { return 1f; }
    protected virtual Vector3 GetAdditionalImpactScale(ItemEffectContext context) { return Vector3.one; }
    #endregion
}
