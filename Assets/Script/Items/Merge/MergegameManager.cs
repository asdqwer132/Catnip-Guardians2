using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class MergeSeries
{
    public int tier;
    public List<ItemRecipeData> itemRecipeDatas;
}

[Serializable]
public class MergeItemMaterial
{
    public MergeItem MergeItem;
    public int count;
}
public class MergegameManager : MonoBehaviour
{
    public GameObject itemPrefab;
    public ItemSeries itemSeries;
    public List<MergeSeries> mergeRecipe;
    public List<MergeItemMaterial> mergeMaterials;

    public MergeItemMaterial currentMaterial;

    //인풋은 최신버전 쓸것

    /*
     * 스페이스 키를 누르면 선택된 머지 아이템으로 생성(오브젝트 풀링 사용)
     * 
     * 옵션 : 
     * 다음 생성까지 기다릴 시간
     * 최대 소환개수
    */

    /* q키를 눌러 매터리얼을 변경
        현재 매터리얼 표시
     */
}
