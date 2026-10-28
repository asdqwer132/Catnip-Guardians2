using UnityEngine;
using UnityEngine.UI;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MergeMaterialIconInjecter : MonoBehaviour
{
    [SerializeField] private ItemData[] itemDatas;
    [SerializeField] private GameObject imageRoot;
    [SerializeField] private Image[] images;

    [ContextMenu("Inject Icon with ItemData")]
    public void InjectIcon()
    {
        if (imageRoot == null || itemDatas == null)
            return;

#if UNITY_EDITOR
        Undo.RecordObject(this, "Find Icon Images");
#endif

        images = imageRoot.GetComponentsInChildren<Image>(true)
            .Where(image => image.gameObject.name == "Icon")
            .ToArray();

#if UNITY_EDITOR
        if (PrefabUtility.IsPartOfPrefabInstance(this))
            PrefabUtility.RecordPrefabInstancePropertyModifications(this);
#endif

        for (int i = 0; i < itemDatas.Length && i < images.Length; i++)
        {
            if (itemDatas[i] == null)
                continue;

            Image image = images[i];

#if UNITY_EDITOR
            Undo.RecordObject(image, "Inject Icon");
#endif

            image.sprite = itemDatas[i].icon;

#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabInstance(image))
                PrefabUtility.RecordPrefabInstancePropertyModifications(image);
#endif
        }
    }
}