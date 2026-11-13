using UnityEngine;
using UnityEngine.UI;

public class SkillLevelSelecter : MonoBehaviour
{
    [SerializeField] Image levelImage;
    [SerializeField] Sprite[] sprites;


    public void SetLevel(int index)
    {
        if (index == -1) {
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
    }
}
