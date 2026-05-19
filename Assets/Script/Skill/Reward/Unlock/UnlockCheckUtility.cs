using UnityEngine;

public static class UnlockCheckUtility
{
    public static bool CanUse(IUnlockable unlockable)
    {
        if (unlockable == null)
            return false;

        if (!unlockable.RequireUnlock)
            return true;

        if (string.IsNullOrEmpty(unlockable.UnlockId))
        {
            Debug.LogWarning("UnlockId가 비어있습니다: ");
            return false;
        }

        if (UnlockManager.Instance == null)
        {
            Debug.LogWarning("UnlockManager.Instance가 없습니다. 해금 검사를 통과시킵니다.");
            return true;
        }

        bool unlocked = UnlockManager.Instance.IsUnlocked(
            unlockable.UnlockType,
            unlockable.UnlockId
        );

        if (!unlocked)
        {
            Debug.Log(
                "잠겨 있습니다: " +
                unlockable.UnlockType +
                " / " +
                unlockable.UnlockId 
            );
        }

        return unlocked;
    }

    //public static bool IsUnlocked(DataType type, string unlockId)
    //{
    //    if (string.IsNullOrEmpty(unlockId))
    //        return false;

    //    if (UnlockManager.Instance == null)
    //    {
    //        Debug.LogWarning("UnlockManager.Instance가 없습니다. 해금 검사를 통과시킵니다.");
    //        return true;
    //    }

    //    return UnlockManager.Instance.IsUnlocked(type, unlockId);
    //}
}