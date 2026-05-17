using UnityEngine;
using UnityEngine.UI;

public class Plant : MonoBehaviour
{
    public PlantUI plantUI;

    public GameManager gameManager;
    public PlantData plantData;

    public Slider healthSlider;
    public Slider growthSlider;

    private float currentHP;
    private float growTimer;

    void Update()
    {
        //타이머 수정
        growTimer += Time.deltaTime;
        growthSlider.value = growTimer;

        if (growTimer >= plantData.growTime)
        {
            gameManager.Victory();
        }
    }

    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        healthSlider.value = currentHP;

        if (currentHP <= 0)
        {
            gameManager.GameOver();
        }
    }

    //체력등 기다 리셋
    public void Init()
    {
        if (PlantManager.instance != null && PlantManager.instance.CurrentPlant != null)
        {
            plantData = PlantManager.instance.CurrentPlant;
        }
        plantUI.SetPlantData(plantData);

        currentHP = plantData.maxHP;

        healthSlider.maxValue = plantData.maxHP;
        healthSlider.value = currentHP;

        growthSlider.maxValue = plantData.growTime;
        growthSlider.value = 0f;
        growTimer = 0f;
    }
}