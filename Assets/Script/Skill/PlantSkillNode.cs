using UnityEngine;

public class PlantSkillNode : MonoBehaviour, ISkillNode
{
    [Header("Plant Target")]
    public PlantData plantData;

    [Header("Upgrade")]
    public PlantSkillType skillType;
    public float value = 1f;

    public void ApplyEffect()
    {
        if (plantData == null)
        {
            Debug.LogWarning("PlantData가 연결되지 않았습니다.");
            return;
        }

        switch (skillType)
        {
            case PlantSkillType.MaxHP:
                plantData.maxHP += value;
                break;

            case PlantSkillType.GrowTime:
                plantData.growTime -= value;
                plantData.growTime = Mathf.Max(1f, plantData.growTime);
                break;

            case PlantSkillType.RewardAmount:
                plantData.rewardAmount += Mathf.RoundToInt(value);
                break;
            case PlantSkillType.Open:
                if (PlantManager.instance != null)
                {
                    PlantManager.instance.UnlockPlant(plantData);
                }
                break;
        }
    }
}