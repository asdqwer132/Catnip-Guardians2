#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(UseItemStatusReactionModule))]
public sealed class UseItemStatusReactionModuleEditor : Editor
{
    private static readonly string[] throwFields = { "throwStartAnchor", "throwTargetAnchor", "throwOffsetSpace",
        "throwStartOffset", "throwTargetOffset", "targetPlacement", "directionMode", "fixedWorldDirection", "directionAngle",
        "forwardOffset", "sideOffset", "radius", "spreadAngle", "shotgunRadiusOffset", "shotgunDistribution",
        "spreadStartMode", "clockwiseSpread", "throwOptions" };
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allowedSeries"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("allowedItems"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("excludedItems"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("excludedItemIds"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("probability"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("itemSource"), true);
        if (serializedObject.FindProperty("itemSource").intValue == (int)StatusReactionItemSource.ConfiguredItem)
            EditorGUILayout.PropertyField(serializedObject.FindProperty("configuredItem"), true);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("delivery"), true);
        if (serializedObject.FindProperty("delivery").intValue == (int)StatusReactionItemDelivery.ThrowAtImpact)
        {
            foreach (string field in throwFields) EditorGUILayout.PropertyField(serializedObject.FindProperty(field), true);
            EditorGUILayout.HelpBox("Used Item / Configured Item 모두 투척할 수 있습니다. Throw Options를 펼쳐 이 상태의 프리팹·모션을 설정하세요. 프리팹 모션을 그대로 쓰려면 Override Motion을 끕니다.", MessageType.Info);
        }
        EditorGUILayout.PropertyField(serializedObject.FindProperty("consumeUseBuffs"), true);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
