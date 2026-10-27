using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class MergeMaterialIconInjecter : MonoBehaviour
{
    [SerializeField] ItemData[] itemDatas;
    [SerializeField] GameObject imageRoot;
    [SerializeField] Image[] images;

    [ContextMenu("Inject Icon with ItemData")]

    public void InjectIcon()
    {
        images = imageRoot.GetComponentsInChildren<Image>(true)
            .Where(image => image.gameObject.name == "Icon")
            .ToArray();

        for (int i = 0; i < itemDatas.Length && i < images.Length; i++)
        {
            images[i].sprite = itemDatas[i].icon;
        }
    }
}
