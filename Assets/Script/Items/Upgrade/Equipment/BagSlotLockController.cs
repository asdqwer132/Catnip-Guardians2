using System;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;

[Serializable]
public class LockInfo : IUnlockable
{
    public string unlockId;
    public bool locked = true;

    public bool RequireUnlock => true;

    public DataType UnlockType => DataType.BagSlot;

    public string UnlockId => unlockId;
}