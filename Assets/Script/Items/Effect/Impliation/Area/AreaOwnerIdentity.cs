using UnityEngine;

[DisallowMultipleComponent]
public sealed class AreaOwnerIdentity : MonoBehaviour
{
    [Tooltip("같은 비어 있지 않은 키의 소유자만 아군 장판으로 판정합니다.")]
    public string alliedGroup;

    internal static bool AreAllied(GameObject first, GameObject second)
    {
        if (first == null || second == null) return false;
        if (first == second) return true;
        AreaOwnerIdentity a = first.GetComponentInParent<AreaOwnerIdentity>();
        AreaOwnerIdentity b = second.GetComponentInParent<AreaOwnerIdentity>();
        return a != null && b != null && !string.IsNullOrWhiteSpace(a.alliedGroup) &&
            string.Equals(a.alliedGroup.Trim(), b.alliedGroup != null ? b.alliedGroup.Trim() : null,
                System.StringComparison.Ordinal);
    }
}
