using UnityEngine;

public class MergeGimicManager : MonoBehaviour
{
    [SerializeField] MergeBoxGimic[] mergeBoxGimics;
    [SerializeField] CoolButton[] skillButtons;
    private MergeBoxGimic currentBox;


    public void SetSkills()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            skillButtons[i].button.onClick.AddListener(() => currentBox.UseBoxSkill(i));
        }
    }
    public void SetBox(int index) => currentBox = mergeBoxGimics[index];
}
