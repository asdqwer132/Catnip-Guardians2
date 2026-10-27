using System.Collections.Generic;
using UnityEngine;

public class MergeItemInjecter : MonoBehaviour
{
    [Header("Manager")]
    [SerializeField] private MergegameManager mergegameManager;

    [Header("Datas")]
    [SerializeField] private MergeItem[] mergeItems;
    [SerializeField] private GameObject mergeUIRoot;
    [SerializeField] private MergeUI[] mergeUIs;

    [Header("Settings")]
    [SerializeField, Min(0)] private int count = 10;
    [SerializeField, Min(0)] private int defaultCount = 10;

    [ContextMenu("Inject")]
    public void Inject()
    {
        if (mergegameManager == null || mergeUIRoot == null || mergeItems == null)
        {
            Debug.LogError("MergeItemInjecter: Manager / Merge UI Root / Merge Items를 연결하세요.", this);
            return;
        }
        // 재료가 없어서 숨겨진 슬롯도 다음 주입에 같은 순서로 다시 연결합니다.
        var slots = mergeUIRoot.GetComponentsInChildren<MergeUI>(true);
        if (slots.Length < mergeItems.Length)
        {
            Debug.LogError("MergeItemInjecter: Merge Items 개수만큼 MergeUI 슬롯이 필요합니다. 기존 데이터는 유지합니다.", this);
            return;
        }
        var materials = new List<MergeItemMaterial>(mergeItems.Length);
        for (int i = 0; i < mergeItems.Length; i++)
            materials.Add(new MergeItemMaterial(mergeItems[i], slots[i], Mathf.Max(0, count), Mathf.Max(0, defaultCount)));
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.Undo.RecordObject(this, "Inject merge materials");
            UnityEditor.Undo.RecordObject(mergegameManager, "Inject merge materials");
        }
#endif
        mergeUIs = slots;
        mergegameManager.mergeItems = new List<MergeItem>(mergeItems);
        mergegameManager.mergeMaterials = materials;
        mergegameManager.RefreshUI();
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.EditorUtility.SetDirty(mergegameManager);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(this);
            UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(mergegameManager);
            if (gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            if (mergegameManager.gameObject.scene.IsValid()) UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(mergegameManager.gameObject.scene);
        }
#endif
    }
}
