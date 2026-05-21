using UnityEngine;

[CreateAssetMenu(fileName = "Recipe", menuName = "Game/Recipe")]
public class ItemRecipeData : ScriptableObject
{
    public RecipeMaterial[] materials;

    public ItemData resultItem;
}

[System.Serializable]
public class RecipeMaterial
{
    public ItemData itemData;
    public int amount;
}