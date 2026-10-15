using TMPro;
using UnityEngine;

[CreateAssetMenu(menuName = "05.Merge Game/Catalog", fileName = "MergeGameCatalog")]
public sealed class MergeGameCatalog : ScriptableObject
{
    public MergeContainerDefinition[] containers;
    public SkillNodeData[] containerSkills;
    public ItemData[] encyclopediaItems;
    public EnemyStatData[] encyclopediaEnemies;
    public TMP_FontAsset font;
    [Tooltip("새 통/사전 UI의 한글 글꼴입니다. 원본 픽셀 글꼴은 기존 UI에서 유지합니다.")]
    public Font unicodeFont;
    private TMP_FontAsset runtimeFont;
    public TMP_FontAsset UIFont
    {
        get
        {
            if (unicodeFont == null) return font;
            if (runtimeFont == null)
            {
                runtimeFont = TMP_FontAsset.CreateFontAsset(unicodeFont);
                runtimeFont.isMultiAtlasTexturesEnabled = true;
            }
            return runtimeFont;
        }
    }
    [Header("재료 공급 / 선택")]
    public MergeMaterialSupply supplyMode;
    public MergeSelectionDelivery selectionDelivery;
    [Tooltip("켜면 선택하지 않은 통도 재료를 투입합니다. 통을 바꿔도 이미 떨어진 아이템은 유지됩니다.")]
    public bool injectIntoInactiveContainers;
    [Header("전투 투척")]
    [Min(0f)] public float scatterRadius = 0.35f;
    public bool throwFromOwner;
    public Vector3 throwStartOffset;
}
