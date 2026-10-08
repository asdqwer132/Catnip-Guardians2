using UnityEngine;

[CreateAssetMenu(fileName = "StatusDefinition", menuName = "GameData/Items/Status/Definition")]
public sealed class StatusDefinition : ScriptableObject
{
    public string displayName;
    public bool harmful;
    public bool dispellable = true;
    public string[] interactionTags;
}
