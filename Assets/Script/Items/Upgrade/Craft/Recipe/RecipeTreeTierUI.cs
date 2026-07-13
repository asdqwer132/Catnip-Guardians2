using UnityEngine;

public class RecipeTreeTierUI : MonoBehaviour
{
    [Header("Slot Parent")]
    public RectTransform slotRoot;

    public RectTransform SlotRoot => slotRoot != null ? slotRoot : transform as RectTransform;
}
