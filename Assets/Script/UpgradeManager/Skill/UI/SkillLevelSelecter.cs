using UnityEngine;
using UnityEngine.UI;

public class SkillLevelSelecter : MonoBehaviour
{
    [SerializeField] Image levelImage;
    [SerializeField] Sprite[] sprites;


    public void SetNodeLevel(int level) => SetLevel(level - 1);

    public void SetLevel(int index)
    {
        if (levelImage == null) return;
        if (index < 0 || sprites == null) {
            levelImage.gameObject.SetActive(false);
            return;
        }
        if (index >= sprites.Length)
        {
            levelImage.gameObject.SetActive(false);
            return;
        }
        levelImage.gameObject.SetActive(true);
        levelImage.sprite = sprites[index];
        levelImage.gameObject.SetActive(sprites[index] != null);
    }
}
