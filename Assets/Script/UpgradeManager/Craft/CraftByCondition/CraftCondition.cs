using UnityEngine;

/// <summary>Implement this to add a new kind of crafting condition.</summary>
public abstract class CraftCondition : ScriptableObject
{
    public abstract bool IsMet(CraftContext context);
}
